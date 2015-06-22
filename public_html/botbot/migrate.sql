CREATE VIEW logs_log AS
 SELECT logs.id,
    logs."time" AS "timestamp",
    logs.nick,
    logs.contents AS text,
    logs.act AS action,
    'PRIVMSG'::text AS command,
    logs.contents AS raw,
    logs.channel AS room,
    0 AS search_index,
    0 AS bot_id,
    bots_channel.id AS channel_id,
    logs.host
   FROM logs,
    bots_channel
  WHERE ((((logs.channel)::text IN ( SELECT active.channel
           FROM active)) AND (logs.type = 0)) AND ((logs.channel)::text = bots_channel.name));





CREATE VIEW bots_channel_temp AS
 SELECT channels_sorted.channel_id AS id,
    now() AS created,
    now() AS updated,
    channels_sorted.channel AS name,
    slug(channels_sorted.channel) AS slug,
    NULL::unknown AS private_slug,
    NULL::unknown AS password,
    true AS is_public,
    true AS is_active,
    false AS is_pending,
    (( SELECT count(1) AS count
           FROM active
          WHERE ((active.channel)::text = channels_sorted.channel)) > 0) AS is_featured,
    'dcdaa86e-424a-45ef-8835-9037b4373b6a' AS fingerprint,
    true AS public_kudos,
    1 AS chatbot_id,
    '' AS notes
   FROM ( SELECT row_number() OVER (ORDER BY channels.channel) AS channel_id,
            channels.channel
           FROM ( SELECT DISTINCT logs_meta.channel
                   FROM logs_meta
                  WHERE ((logs_meta.value = 'True'::text) AND (logs_meta.name = 'enabled'::text))) channels) channels_sorted;


