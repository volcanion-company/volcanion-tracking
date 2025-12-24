-- Initialize databases for Write and Read sides
CREATE DATABASE volcanion_tracking_write;
CREATE DATABASE volcanion_tracking_read;

-- Connect to write database
\c volcanion_tracking_write;

-- Create write schema
CREATE SCHEMA IF NOT EXISTS write;

-- Connect to read database
\c volcanion_tracking_read;

-- Create read schema
CREATE SCHEMA IF NOT EXISTS read;
