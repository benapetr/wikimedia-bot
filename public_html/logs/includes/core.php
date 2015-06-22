<?php

require("config.php");
class Logs
{
    public static $channel = NULL;
    public static $pg;

    public static function Init()
    {
        global $pg_us, $pg_pw, $pg_db, $pg_sv;
        self::$pg = pg_connect( 'host=' . $pg_sv . ' dbname=' . $pg_db . ' user=' . $pg_us . ' password='. $pg_pw )
                   or die( 'Could not connect: ' . pg_last_error() );
    }

    public static function GetChannels()
    {
        global $recent;
        $list = array();
        if (isset($_GET['recent']))
        {
            if ($_GET['recent'] == "yes")
                $recent = true;
            else
                $recent = false;
            setcookie("recent", $_GET["recent"]);
        }
        if ($recent === NULL && isset($_COOKIE['recent']) && $_COOKIE['recent'] == "yes")
            $recent = true;
        if ($recent)
            $query = pg_query("SELECT channel FROM active ORDER by channel");
        else
            $query = pg_query("SELECT channel FROM logs_meta WHERE name = 'enabled' AND value = 'True' ORDER by channel");
        while($item = pg_fetch_assoc($query))
        {
            $list[] = $item["channel"];
        }
        return $list;
    }
}
