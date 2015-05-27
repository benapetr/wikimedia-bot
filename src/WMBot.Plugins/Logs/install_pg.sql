--
-- Name: logs; Type: TABLE; Schema: public; Owner: wm-bot; Tablespace: 
--

CREATE TABLE logs (
    id integer NOT NULL,
    channel character varying(200),
    nick character varying(200),
    "time" timestamp without time zone,
    act boolean,
    contents text,
    type integer,
    host character varying(80)
);

CREATE SEQUENCE logs_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

ALTER SEQUENCE logs_id_seq OWNED BY logs.id;
ALTER TABLE ONLY logs ALTER COLUMN id SET DEFAULT nextval('logs_id_seq'::regclass);
ALTER TABLE ONLY logs
    ADD CONSTRAINT logs_pkey PRIMARY KEY (id);
CREATE INDEX logs_channel_ix ON logs USING btree (channel);
CREATE INDEX logs_id_ix ON logs USING btree (id);
CREATE INDEX logs_nick_ix ON logs USING btree (nick);
CREATE INDEX logs_time_ix ON logs USING btree ("time");
CREATE INDEX logs_type_ix ON logs USING btree (type);
