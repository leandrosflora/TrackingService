CREATE TABLE tracking_events (
    id UUID PRIMARY KEY,
    shipment_id UUID NOT NULL,
    order_id UUID NOT NULL,
    buyer_id UUID NOT NULL,
    provider_event_id VARCHAR(200) NOT NULL,
    tracking_code VARCHAR(200) NOT NULL,
    carrier_code VARCHAR(80) NOT NULL,
    carrier_sequence INTEGER NULL,
    status VARCHAR(50) NOT NULL,
    description VARCHAR(1000) NULL,
    exception_code VARCHAR(100) NULL,
    facility_code VARCHAR(100) NULL,
    location_city VARCHAR(200) NULL,
    location_state VARCHAR(100) NULL,
    location_country VARCHAR(10) NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    received_at TIMESTAMPTZ NOT NULL,
    estimated_delivery_date DATE NULL,
    CONSTRAINT uq_tracking_event_provider UNIQUE (carrier_code, provider_event_id)
);

CREATE INDEX idx_tracking_events_shipment ON tracking_events (shipment_id, occurred_at);

CREATE TABLE shipment_tracking (
    shipment_id UUID PRIMARY KEY,
    order_id UUID NOT NULL,
    buyer_id UUID NOT NULL,
    tracking_code VARCHAR(200) NOT NULL,
    carrier_code VARCHAR(80) NOT NULL,
    current_status VARCHAR(50) NULL,
    last_event_id UUID NULL,
    last_carrier_sequence INTEGER NULL,
    last_event_occurred_at TIMESTAMPTZ NULL,
    last_event_received_at TIMESTAMPTZ NULL,
    last_facility_code VARCHAR(100) NULL,
    last_location_city VARCHAR(200) NULL,
    last_location_state VARCHAR(100) NULL,
    last_location_country VARCHAR(10) NULL,
    estimated_delivery_date DATE NULL,
    delivered_at TIMESTAMPTZ NULL,
    current_exception_code VARCHAR(100) NULL,
    version INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT uq_shipment_tracking_code UNIQUE (tracking_code)
);

CREATE TABLE inbox_messages (
    message_id UUID PRIMARY KEY,
    message_type VARCHAR(200) NOT NULL,
    processed_at TIMESTAMPTZ NULL
);

CREATE TABLE outbox_messages (
    id UUID PRIMARY KEY,
    topic VARCHAR(200) NOT NULL,
    message_type VARCHAR(200) NOT NULL,
    aggregate_key VARCHAR(100) NOT NULL,
    payload JSONB NULL,
    created_at TIMESTAMPTZ NOT NULL,
    processed_at TIMESTAMPTZ NULL,
    attempts INTEGER NOT NULL DEFAULT 0,
    next_attempt_at TIMESTAMPTZ NULL
);

CREATE INDEX idx_outbox_messages_dispatch ON outbox_messages (processed_at, next_attempt_at, created_at);
