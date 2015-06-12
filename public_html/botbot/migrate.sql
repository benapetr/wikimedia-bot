create view logs_log as (
SELECT logs.id,
       logs.time as "timestamp",
       logs.nick,
       logs.contents as "text",
       logs.act as "action",
       '' as "command",
       logs.contents as "raw",
       logs.channel as "room",
       0 as "search_index",
       0 as bot_id,
       bots_channel.id as "channel_id",
       logs.host as "host" FROM logs, bots_channel
);


create view bots_channel as (
    SELECT channel_id as "id",
           current_timestamp as "created",
           current_timestamp as "updated",
           channel as "name",
           channel_id::text as "slug",
           NULL as "private_slug",
           NULL as "password",
           TRUE as "is_public",
           TRUE as "is_active",
           FALSE as "is_pending",
           TRUE as "is_featured",
           'dcdaa86e-424a-45ef-8835-9037b4373b6a' as "fingerprint",
           TRUE as "public_kudos",
           1 as "chatbot_id",
           '' as "notes"
    FROM (
        SELECT row_number() OVER(ORDER by channel ASC) as "channel_id",
               channel
        FROM (
            SELECT DISTINCT(channel) FROM logs_meta WHERE value = 'True' AND name = 'enabled'
        ) AS channels
    ) AS channels_sorted
);
