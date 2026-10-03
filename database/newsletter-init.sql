CREATE TABLE IF NOT EXISTS subscribers
(
    id          UUID PRIMARY KEY,
    email       VARCHAR(320) NOT NULL UNIQUE,
    created_utc TIMESTAMPTZ NOT NULL
    );