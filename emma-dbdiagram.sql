/* Emma AI Platform Database Schema for dbdiagram.io
   Generated on 2025-06-02 */

/* Enum Types */
enum contact_type {
  INDIVIDUAL
  ORGANIZATION
  LEAD
}

enum interaction_type {
  EMAIL
  CALL
  MEETING
  MESSAGE
  NOTE
  DOCUMENT
}

enum privacy_tag {
  CRM
  PERSONAL
  PRIVATE
  BUSINESS
  PUBLIC
}

/* Tables */
Table contacts {
  id UUID [pk, default: "uuid_generate_v4()"]
  display_name VARCHAR(100) [not null, note: "Full name of the contact or organization"]
  organization_name VARCHAR(100) [note: "Organization name if applicable"]
  contact_type contact_type [not null, note: "Type of contact: INDIVIDUAL, ORGANIZATION, or LEAD"]
  email VARCHAR(255) [note: "Primary email address"]
  phone VARCHAR(50) [note: "Primary phone number"]
  address TEXT [note: "Physical address"]
  website VARCHAR(255) [note: "Website URL"]
  linkedin_url VARCHAR(255) [note: "LinkedIn profile URL"]
  twitter_handle VARCHAR(50) [note: "Twitter handle"]
  notes TEXT [note: "General notes about the contact"]
  legacy_privacy_tags privacy_tag[] [note: "Legacy field for privacy tags, being migrated to interaction level"]
  created_at TIMESTAMP [default: "CURRENT_TIMESTAMP", note: "Record creation timestamp"]
  updated_at TIMESTAMP [default: "CURRENT_TIMESTAMP", note: "Record last update timestamp"]
  deleted_at TIMESTAMP [note: "Soft delete timestamp, NULL if active"]

  Note: "Central entity that stores all contacts in the Emma AI Platform. Note that privacy_tags are being migrated from this table to the interactions table."
  
  indexes {
    email [name: "idx_contacts_email"]
  }
}

Table interactions {
  id UUID [pk, default: "uuid_generate_v4()"]
  contact_id UUID [ref: > contacts.id, note: "Foreign key to the associated contact"]
  interaction_type interaction_type [not null, note: "Type of interaction: EMAIL, CALL, MEETING, MESSAGE, NOTE, or DOCUMENT"]
  title VARCHAR(255) [not null, note: "Short title or subject of the interaction"]
  content TEXT [note: "Main content or body of the interaction"]
  summary TEXT [note: "Brief summary of the interaction, often AI-generated"]
  occurred_at TIMESTAMP [not null, note: "When the interaction occurred"]
  privacy_tags privacy_tag[] [not null, default: "{}", note: "Array of privacy tags for business logic and access control"]
  sentiment_score DECIMAL(3,2) [note: "AI-generated sentiment score from -1.0 to 1.0"]
  is_favorited BOOLEAN [default: "FALSE", note: "Whether the user has marked this as a favorite"]
  created_at TIMESTAMP [default: "CURRENT_TIMESTAMP", note: "Record creation timestamp"]
  updated_at TIMESTAMP [default: "CURRENT_TIMESTAMP", note: "Record last update timestamp"]
  deleted_at TIMESTAMP [note: "Soft delete timestamp, NULL if active"]

  Note: "Core table for all communication and interactions. Privacy/business logic tags are stored at this level."
  
  indexes {
    contact_id [name: "idx_interactions_contact_id"]
    occurred_at [name: "idx_interactions_occurred_at"]
  }
}

Table email_details {
  interaction_id UUID [pk, ref: - interactions.id, note: "Foreign key to the associated interaction"]
  subject VARCHAR(255) [not null, note: "Email subject line"]
  sender VARCHAR(255) [not null, note: "Email sender address"]
  recipients TEXT[] [not null, note: "Array of primary recipient addresses"]
  cc_recipients TEXT[] [note: "Array of CC recipient addresses"]
  bcc_recipients TEXT[] [note: "Array of BCC recipient addresses"]
  thread_id VARCHAR(255) [note: "Email thread identifier"]
  in_reply_to VARCHAR(255) [note: "Reference to the email this is replying to"]
  has_attachments BOOLEAN [default: "FALSE", note: "Whether the email has attachments"]
  importance VARCHAR(20) [note: "Email importance flag (HIGH, NORMAL, LOW)"]

  Note: "Contains email-specific metadata linked to the main interactions table"
}

Table call_details {
  interaction_id UUID [pk, ref: - interactions.id, note: "Foreign key to the associated interaction"]
  duration_seconds INTEGER [note: "Call duration in seconds"]
  direction VARCHAR(10) [note: "Whether the call was incoming or outgoing (INBOUND, OUTBOUND)"]
  call_status VARCHAR(20) [note: "Status of the call (COMPLETED, MISSED, VOICEMAIL, SCHEDULED)"]
  phone_number VARCHAR(50) [note: "Phone number of the other party"]
  recording_url VARCHAR(255) [note: "URL to call recording if available"]

  Note: "Contains call-specific metadata linked to the main interactions table"
}

Table meeting_details {
  interaction_id UUID [pk, ref: - interactions.id, note: "Foreign key to the associated interaction"]
  location VARCHAR(255) [note: "Physical or virtual meeting location"]
  start_time TIMESTAMP [not null, note: "Meeting start time"]
  end_time TIMESTAMP [not null, note: "Meeting end time"]
  attendees TEXT[] [note: "Array of attendee email addresses"]
  meeting_url VARCHAR(255) [note: "URL for virtual meetings"]
  is_recurring BOOLEAN [default: "FALSE", note: "Whether this is a recurring meeting"]
  recurrence_pattern VARCHAR(100) [note: "Description of recurrence pattern if applicable"]

  Note: "Contains meeting-specific metadata linked to the main interactions table"
}

Table document_details {
  interaction_id UUID [pk, ref: - interactions.id, note: "Foreign key to the associated interaction"]
  document_type VARCHAR(50) [not null, note: "Type of document (contract, listing, etc.)"]
  filename VARCHAR(255) [not null, note: "Original filename"]
  file_size_bytes BIGINT [note: "Size of the file in bytes"]
  mime_type VARCHAR(100) [note: "MIME type of the document"]
  storage_url VARCHAR(255) [note: "URL where the document is stored"]
  document_date TIMESTAMP [note: "Date associated with the document content"]

  Note: "Contains document-specific metadata linked to the main interactions table"
}

Table tags {
  id UUID [pk, default: "uuid_generate_v4()"]
  name VARCHAR(50) [not null, unique, note: "Tag name"]
  color VARCHAR(7) [note: "Hexadecimal color code for UI display"]
  description VARCHAR(255) [note: "Description of the tag's purpose"]
  created_at TIMESTAMP [default: "CURRENT_TIMESTAMP", note: "Record creation timestamp"]

  Note: "Reusable tags for categorizing interactions"
}

Table interaction_tags {
  interaction_id UUID [ref: > interactions.id, note: "Foreign key to the interaction"]
  tag_id UUID [ref: > tags.id, note: "Foreign key to the tag"]
  created_at TIMESTAMP [default: "CURRENT_TIMESTAMP", note: "Record creation timestamp"]

  indexes {
    (interaction_id, tag_id) [pk]
  }

  Note: "Junction table implementing many-to-many relationship between interactions and tags"
}

Table ai_analysis {
  id UUID [pk, default: "uuid_generate_v4()"]
  interaction_id UUID [ref: > interactions.id, note: "Foreign key to the analyzed interaction"]
  analysis_type VARCHAR(50) [not null, note: "Type of AI analysis performed"]
  result JSONB [not null, note: "JSON result of the analysis"]
  confidence_score DECIMAL(5,4) [note: "Confidence level of the analysis"]
  model_version VARCHAR(50) [note: "Version of the AI model used"]
  created_at TIMESTAMP [default: "CURRENT_TIMESTAMP", note: "Record creation timestamp"]

  Note: "Stores results from various AI analyses performed on interaction content"
}

Table users {
  id UUID [pk, default: "uuid_generate_v4()"]
  username VARCHAR(50) [not null, unique, note: "Username for login"]
  email VARCHAR(255) [not null, unique, note: "Email address for the user"]
  password_hash VARCHAR(255) [not null, note: "Hashed password"]
  first_name VARCHAR(50) [note: "User's first name"]
  last_name VARCHAR(50) [note: "User's last name"]
  role VARCHAR(20) [not null, note: "User role for access control"]
  is_active BOOLEAN [default: "TRUE", note: "Whether the user account is active"]
  last_login_at TIMESTAMP [note: "Timestamp of last successful login"]
  created_at TIMESTAMP [default: "CURRENT_TIMESTAMP", note: "Record creation timestamp"]
  updated_at TIMESTAMP [default: "CURRENT_TIMESTAMP", note: "Record last update timestamp"]

  Note: "Stores user accounts for the Emma AI Platform"
}

/* 
View Definition (for reference - not directly supported in dbdiagram.io)
View: vw_interaction_details
Description: View for easy access to interactions with contact details
Query: SELECT i.id AS interaction_id, i.title, i.interaction_type, i.content, i.summary, i.occurred_at, i.privacy_tags, i.sentiment_score, c.id AS contact_id, c.display_name AS contact_name, c.email AS contact_email, c.organization_name FROM interactions i JOIN contacts c ON i.contact_id = c.id WHERE i.deleted_at IS NULL AND c.deleted_at IS NULL
*/

/*
Migration Notes
Status: in-progress
Description: Migration of privacy/business logic tags from Contact to Interaction entities
Backward Compatibility: true
Legacy Fields: contacts.legacy_privacy_tags
New Fields: interactions.privacy_tags
Notes: For a transition period, backward compatibility is maintained. Legacy logic and data structures will be supported until migration is complete.
*/

/*
Recommended Indexing (for reference)
Table: interactions, Columns: privacy_tags, Type: GIN, Description: For efficient querying of array values when filtering by privacy tags
Table: ai_analysis, Columns: result, Type: GIN, Description: For querying into JSONB data
*/

/*
Performance Tips:
- Consider partitioning the interactions table by date for large datasets
- Regularly analyze the database to update statistics for the query planner
- Monitor and vacuum tables with frequent updates to manage bloat
*/
