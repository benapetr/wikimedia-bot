drop view bots_channel;

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
