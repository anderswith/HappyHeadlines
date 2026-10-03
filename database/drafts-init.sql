CREATE TABLE drafts (
    id uuid PRIMARY KEY,
    author varchar(100) NOT NULL
        CHECK (length(trim(author)) > 0),
    title varchar(200) NOT NULL,
    content text NOT NULL,
    created_utc timestamptz NOT NULL,
    updated_utc timestamptz NOT NULL
);