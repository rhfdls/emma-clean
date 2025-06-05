-- Emma AI Platform Database Schema
-- Complete implementation with privacy/business logic tags on Interaction entities

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create enum types for various categorizations
CREATE TYPE contact_type AS ENUM ('INDIVIDUAL', 'ORGANIZATION', 'LEAD');
CREATE TYPE interaction_type AS ENUM ('EMAIL', 'CALL', 'MEETING', 'MESSAGE', 'NOTE', 'DOCUMENT');
CREATE TYPE privacy_tag AS ENUM ('CRM', 'PERSONAL', 'PRIVATE', 'BUSINESS', 'PUBLIC');

-- Contact table - Central entity for people and organizations
CREATE TABLE contacts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    display_name VARCHAR(100) NOT NULL,
    organization_name VARCHAR(100),
    contact_type contact_type NOT NULL,
    email VARCHAR(255),
    phone VARCHAR(50),
    address TEXT,
    website VARCHAR(255),
    linkedin_url VARCHAR(255),
    twitter_handle VARCHAR(50),
    notes TEXT,
    -- Legacy fields (will be used only for backward compatibility)
    legacy_privacy_tags privacy_tag[] DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- Create index on contact email for faster lookups
CREATE INDEX idx_contacts_email ON contacts(email);

-- Interaction table - All communication and touchpoints
CREATE TABLE interactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    contact_id UUID REFERENCES contacts(id),
    interaction_type interaction_type NOT NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT,
    summary TEXT,
    occurred_at TIMESTAMP WITH TIME ZONE NOT NULL,
    -- Privacy/business logic tags directly on interaction entities
    privacy_tags privacy_tag[] NOT NULL DEFAULT '{}',
    sentiment_score DECIMAL(3,2),
    is_favorited BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- Create index for faster interaction lookups by contact
CREATE INDEX idx_interactions_contact_id ON interactions(contact_id);
CREATE INDEX idx_interactions_occurred_at ON interactions(occurred_at);

-- Email-specific details for interactions
CREATE TABLE email_details (
    interaction_id UUID PRIMARY KEY REFERENCES interactions(id) ON DELETE CASCADE,
    subject VARCHAR(255) NOT NULL,
    sender VARCHAR(255) NOT NULL,
    recipients TEXT[] NOT NULL,
    cc_recipients TEXT[],
    bcc_recipients TEXT[],
    thread_id VARCHAR(255),
    in_reply_to VARCHAR(255),
    has_attachments BOOLEAN DEFAULT FALSE,
    importance VARCHAR(20)
);

-- Call details for interactions
CREATE TABLE call_details (
    interaction_id UUID PRIMARY KEY REFERENCES interactions(id) ON DELETE CASCADE,
    duration_seconds INTEGER,
    direction VARCHAR(10) CHECK (direction IN ('INBOUND', 'OUTBOUND')),
    call_status VARCHAR(20) CHECK (call_status IN ('COMPLETED', 'MISSED', 'VOICEMAIL', 'SCHEDULED')),
    phone_number VARCHAR(50),
    recording_url VARCHAR(255)
);

-- Meeting details for interactions
CREATE TABLE meeting_details (
    interaction_id UUID PRIMARY KEY REFERENCES interactions(id) ON DELETE CASCADE,
    location VARCHAR(255),
    start_time TIMESTAMP WITH TIME ZONE NOT NULL,
    end_time TIMESTAMP WITH TIME ZONE NOT NULL,
    attendees TEXT[],
    meeting_url VARCHAR(255),
    is_recurring BOOLEAN DEFAULT FALSE,
    recurrence_pattern VARCHAR(100)
);

-- Document details for interactions
CREATE TABLE document_details (
    interaction_id UUID PRIMARY KEY REFERENCES interactions(id) ON DELETE CASCADE,
    document_type VARCHAR(50) NOT NULL,
    filename VARCHAR(255) NOT NULL,
    file_size_bytes BIGINT,
    mime_type VARCHAR(100),
    storage_url VARCHAR(255),
    document_date TIMESTAMP WITH TIME ZONE
);

-- Tags for categorization
CREATE TABLE tags (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL UNIQUE,
    color VARCHAR(7),
    description VARCHAR(255),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Many-to-many relationship between interactions and tags
CREATE TABLE interaction_tags (
    interaction_id UUID REFERENCES interactions(id) ON DELETE CASCADE,
    tag_id UUID REFERENCES tags(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (interaction_id, tag_id)
);

-- AI Analysis results for interactions
CREATE TABLE ai_analysis (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    interaction_id UUID REFERENCES interactions(id) ON DELETE CASCADE,
    analysis_type VARCHAR(50) NOT NULL,
    result JSONB NOT NULL,
    confidence_score DECIMAL(5,4),
    model_version VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User table for Emma AI Platform users
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(50),
    last_name VARCHAR(50),
    role VARCHAR(20) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    last_login_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create automatic update timestamp function
CREATE OR REPLACE FUNCTION update_modified_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updating timestamps
CREATE TRIGGER update_contacts_modtime
BEFORE UPDATE ON contacts
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

CREATE TRIGGER update_interactions_modtime
BEFORE UPDATE ON interactions
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

CREATE TRIGGER update_users_modtime
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION update_modified_column();

-- Create sample data for the Emma AI Platform
INSERT INTO contacts (display_name, contact_type, email, phone, organization_name)
VALUES 
('John Smith', 'INDIVIDUAL', 'john.smith@example.com', '555-123-4567', 'Acme Real Estate'),
('Sarah Johnson', 'INDIVIDUAL', 'sarah.johnson@example.com', '555-765-4321', 'Johnson Properties'),
('Acme Corporation', 'ORGANIZATION', 'info@acmecorp.com', '555-999-8888', 'Acme Corporation');

-- Add sample tags
INSERT INTO tags (name, color, description)
VALUES 
('Follow-up', '#FF5733', 'Requires follow-up action'),
('Important', '#C70039', 'High priority items'),
('Real Estate', '#900C3F', 'Property related discussions'),
('Archived', '#581845', 'Completed and archived items');

-- Add comment with usage instructions
COMMENT ON DATABASE emma IS 'Emma AI Platform main database for real estate professionals';

-- Create view for easy access to interactions with contact details
CREATE OR REPLACE VIEW vw_interaction_details AS
SELECT 
    i.id AS interaction_id,
    i.title,
    i.interaction_type,
    i.content,
    i.summary,
    i.occurred_at,
    i.privacy_tags,
    i.sentiment_score,
    c.id AS contact_id,
    c.display_name AS contact_name,
    c.email AS contact_email,
    c.organization_name
FROM 
    interactions i
JOIN 
    contacts c ON i.contact_id = c.id
WHERE 
    i.deleted_at IS NULL
    AND c.deleted_at IS NULL;

-- Create function to get interactions by privacy tag
CREATE OR REPLACE FUNCTION get_interactions_by_privacy_tag(tag_value privacy_tag)
RETURNS TABLE (
    interaction_id UUID,
    contact_name VARCHAR(100),
    interaction_type interaction_type,
    title VARCHAR(255),
    occurred_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        i.id AS interaction_id,
        c.display_name AS contact_name,
        i.interaction_type,
        i.title,
        i.occurred_at
    FROM 
        interactions i
    JOIN 
        contacts c ON i.contact_id = c.id
    WHERE 
        tag_value = ANY(i.privacy_tags)
        AND i.deleted_at IS NULL
    ORDER BY 
        i.occurred_at DESC;
END;
$$ LANGUAGE plpgsql;

-- Print completion message
DO $$
BEGIN
    RAISE NOTICE 'Emma AI Platform schema created successfully!';
END $$;
