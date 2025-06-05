CREATE TABLE IF NOT EXISTS test_entities (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
INSERT INTO test_entities (name) VALUES ('Test Entity 1');
SELECT * FROM test_entities;
