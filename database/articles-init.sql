CREATE TABLE articles (
    id uuid PRIMARY KEY,
    title varchar(200) NOT NULL
        CHECK (length(trim(title)) > 0),
    content text NOT NULL
        CHECK (length(trim(content)) > 0),
    created_utc timestamptz NOT NULL
);
