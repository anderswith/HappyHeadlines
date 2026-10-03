CREATE TABLE comments (
  id uuid PRIMARY KEY,
  article_id uuid NOT NULL,
  article_region varchar(30) NOT NULL,
  author varchar(100) NOT NULL
      CHECK (length(trim(author)) > 0),
  text text NOT NULL
      CHECK (length(trim(text)) > 0),
  created_utc timestamptz NOT NULL
);

CREATE INDEX ix_comments_article
    ON comments (article_region, article_id);