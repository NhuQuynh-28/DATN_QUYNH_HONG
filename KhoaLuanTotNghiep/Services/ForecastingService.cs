using KhoaLuanTotNghiep.Data;
using KhoaLuanTotNghiep.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KhoaLuanTotNghiep.Services
{
    /// <summary>
    /// ARIMA-based forecasting service.
    /// Implements a simplified ARIMA(1,1,1) model for next-month prediction
    /// per zone, with Memory Cache to avoid retraining on every request.
    /// </summary>
    public class ForecastingService
    {
        private readonly AppDbContext _ctx;
        private readonly IMemoryCache _cache;

        public ForecastingService(AppDbContext ctx, IMemoryCache cache)
        {
            _ctx = ctx;
            _cache = cache;
        }

        // ═══════════════════════════════════════════════════════════
        // Public API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Dự báo số đơn và khách hàng cho tháng kế tiếp của một zone.
        /// </summary>
        public ArimaForecastResult ForecastZone(int zoneId)
        {
            var cacheKey = $"arima_{zoneId}";
            if (_cache.TryGetValue(cacheKey, out ArimaForecastResult? cached) && cached != null)
                return cached;

            var history = _ctx.ZoneHistories
                .Where(h => h.ZoneId == zoneId)
                .OrderBy(h => h.Year).ThenBy(h => h.Month)
                .ToList();

            var orderSeries    = history.Where(h => h.OrdersReal.HasValue)
                                        .Select(h => (double)h.OrdersReal!.Value).ToArray();
            var customerSeries = history.Where(h => h.CustomersReal.HasValue)
                                        .Select(h => (double)h.CustomersReal!.Value).ToArray();

            var orderFc    = RunArima(orderSeries);
            var customerFc = RunArima(customerSeries);

            var result = new ArimaForecastResult
            {
                ZoneId       = zoneId,
                Order        = (int)Math.Max(0, Math.Round(orderFc.Mean)),
                OrderLow     = (int)Math.Max(0, Math.Round(orderFc.Mean - 1.96 * orderFc.Sigma)),
                OrderHigh    = (int)Math.Round(orderFc.Mean + 1.96 * orderFc.Sigma),
                Customer     = (int)Math.Max(0, Math.Round(customerFc.Mean)),
                CustomerLow  = (int)Math.Max(0, Math.Round(customerFc.Mean - 1.96 * customerFc.Sigma)),
                CustomerHigh = (int)Math.Round(customerFc.Mean + 1.96 * customerFc.Sigma),
                Method       = "ARIMA(1,1,1)",
                DataPoints   = orderSeries.Length
            };

            _cache.Set(cacheKey, result, TimeSpan.FromHours(2));
            return result;
        }

        /// <summary>
        /// Dự báo hàng loạt cho nhiều zone (dùng cho phân công tự động).
        /// </summary>
        public Dictionary<int, ArimaForecastResult> ForecastBatch(IEnumerable<int> zoneIds)
        {
            var result = new Dictionary<int, ArimaForecastResult>();
            foreach (var zoneId in zoneIds)
                result[zoneId] = ForecastZone(zoneId);
            return result;
        }

        // ═══════════════════════════════════════════════════════════
        // Core ARIMA(1,d,1) Implementation
        // ═══════════════════════════════════════════════════════════

        private (double Mean, double Sigma) RunArima(double[] series)
        {
            if (series.Length == 0)
                return (0, 0);

            if (series.Length == 1)
                return (series[0], series[0] * 0.1);

            if (series.Length == 2)
            {
                double mean = (series[0] + series[1]) / 2;
                return (series[1], Math.Abs(series[1] - series[0]) * 0.5);
            }

            // ── Step 1: Integration (d=1) — difference once for stationarity
            double[] diff = Difference(series, 1);

            // ── Step 2: Estimate AR(1) coefficient (φ) by OLS
            double phi = EstimateAR1(diff);

            // ── Step 3: Estimate MA(1) coefficient (θ) from residuals
            double[] residuals = ComputeResiduals(diff, phi);
            double theta = EstimateMA1(residuals);

            // ── Step 4: Forecast d(t+1) = φ*d(t) + θ*e(t)
            double lastDiff     = diff[diff.Length - 1];
            double lastResidual = residuals[residuals.Length - 1];
            double forecastDiff = phi * lastDiff + theta * lastResidual;

            // ── Step 5: Invert differencing → forecast in original scale
            double lastOriginal = series[series.Length - 1];
            double forecastRaw  = lastOriginal + forecastDiff;

            // ── Step 6: Sigma — std of residuals (uncertainty)
            double sigma = StandardDeviation(residuals);

            return (forecastRaw, sigma > 0 ? sigma : lastOriginal * 0.05);
        }

        // ── Differencing: compute y[t] - y[t-1]
        private static double[] Difference(double[] series, int order)
        {
            var result = series.ToArray();
            for (int o = 0; o < order; o++)
            {
                var temp = new double[result.Length - 1];
                for (int i = 0; i < temp.Length; i++)
                    temp[i] = result[i + 1] - result[i];
                result = temp;
            }
            return result;
        }

        // ── Estimate AR(1): φ = Σ(d[t]*d[t-1]) / Σ(d[t-1]²)  (Yule-Walker)
        private static double EstimateAR1(double[] diff)
        {
            if (diff.Length < 2) return 0;
            double num = 0, den = 0;
            for (int i = 1; i < diff.Length; i++)
            {
                num += diff[i] * diff[i - 1];
                den += diff[i - 1] * diff[i - 1];
            }
            if (den == 0) return 0;
            double phi = num / den;
            // Clamp to [-0.99, 0.99] for stationarity
            return Math.Max(-0.99, Math.Min(0.99, phi));
        }

        // ── Residuals from AR(1) fit
        private static double[] ComputeResiduals(double[] diff, double phi)
        {
            var res = new double[diff.Length];
            res[0] = diff[0]; // assume e[0] = d[0]
            for (int i = 1; i < diff.Length; i++)
                res[i] = diff[i] - phi * diff[i - 1];
            return res;
        }

        // ── Estimate MA(1): θ ≈ correlation of consecutive residuals
        private static double EstimateMA1(double[] res)
        {
            if (res.Length < 2) return 0;
            double num = 0, den = 0;
            for (int i = 1; i < res.Length; i++)
                num += res[i] * res[i - 1];
            for (int i = 0; i < res.Length; i++)
                den += res[i] * res[i];
            if (den == 0) return 0;
            double theta = num / den;
            return Math.Max(-0.99, Math.Min(0.99, theta));
        }

        // ── Population standard deviation
        private static double StandardDeviation(double[] values)
        {
            if (values.Length == 0) return 0;
            double mean = values.Average();
            double variance = values.Sum(v => (v - mean) * (v - mean)) / values.Length;
            return Math.Sqrt(variance);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DTO
    // ═══════════════════════════════════════════════════════════
    public class ArimaForecastResult
    {
        public int    ZoneId       { get; set; }
        public int    Order        { get; set; }
        public int    OrderLow     { get; set; }
        public int    OrderHigh    { get; set; }
        public int    Customer     { get; set; }
        public int    CustomerLow  { get; set; }
        public int    CustomerHigh { get; set; }
        public string Method       { get; set; } = "ARIMA";
        public int    DataPoints   { get; set; }
    }
}
