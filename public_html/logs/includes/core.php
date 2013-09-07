<?

require("config.php");
require("includes/logshtml.php");
class Logs
{
    public static $title = "Wikimedia irc log browser";
    public static $page = "index";
    public static $channel;
    public static $mysql;

    public static function Init()
    {
        global $mysql_db, $mysql_user, $mysql_host, $mysql_pw;
        self::$mysql = mysql_connect($mysql_host, $mysql_user, $mysql_pw);
        mysql_select_db($mysql_db);
        if (isset($_GET["data"]))
        {
            LogsHtml::$data = "";
        }
        if (isset($_GET["display"]))
        {
            self::$page = "display";
            self::$channel = $_GET["display"];
            return;
        }
    }

    public static function GetChannels()
    {
        $list = array();
        $query = mysql_query("SELECT DISTINCT(channel) FROM logs");
        while($item = mysql_fetch_assoc($query))
        {
            $list[] = $item["channel"];
        }
        return $list;
    }

    public static function Render()
    {
        LogsHtml::Render();
        mysql_close(self::$mysql);
    }
}
