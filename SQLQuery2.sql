﻿CREATE TABLE Images (
    Id INT PRIMARY KEY IDENTITY,
    Category NVARCHAR(50),
    ImageName NVARCHAR(255),
    ImageData VARBINARY(MAX)
);
