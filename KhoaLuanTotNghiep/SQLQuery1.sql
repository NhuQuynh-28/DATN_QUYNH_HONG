ALTER TABLE Users
ADD Email NVARCHAR(100)

UPDATE Users
SET Email='buithinhuquynh553@gmail.com'
WHERE Username='admin' 

ALTER TABLE Users
ADD ResetCode NVARCHAR(10)