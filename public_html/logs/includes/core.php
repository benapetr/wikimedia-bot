<?

require("config.php");
class Logs
{
    public static $title = "Wikimedia irc log browser";
    public static $page = "index";
    private static $channel;
    public static $mysql;

    public static function Init()
    {
        global $mysql_db, $mysql_user, $mysql_host, $mysql_pw;
        self::$mysql = mysql_connect($mysql_host, $mysql_user, $mysql_pw);
        mysql_select_db($mysql_db);
        if (isset($_GET["display"]))
        {
            self::$page = "display";
            self::$channel = $_GET["display"];
            return;
        }
    }

    private static function auto_link_text($text)
    {
        $pattern  = '#\b(([\w-]+://?|www[.])[^\s()<>]+(?:\([\w\d]+\)|([^[:punct:]\s]|/)))#';
        return preg_replace_callback($pattern, 'self::auto_link_text_callback', $text);
    }

    private static function auto_link_text_callback($matches)
    {
        $max_url_length = 250;
        $max_depth_if_over_length = 20;
        $ellipsis = '&hellip;';

        $url_full = $matches[0];
        $url_short = '';

    if (strlen($url_full) > $max_url_length) {
        $parts = parse_url($url_full);
        $url_short = $parts['scheme'] . '://' . preg_replace('/^www\./', '', $parts['host']) . '/';

        $path_components = explode('/', trim($parts['path'], '/'));
        foreach ($path_components as $dir) {
            $url_string_components[] = $dir . '/';
        }

        if (!empty($parts['query'])) {
            $url_string_components[] = '?' . $parts['query'];
        }

        if (!empty($parts['fragment'])) {
            $url_string_components[] = '#' . $parts['fragment'];
        }

        for ($k = 0; $k < count($url_string_components); $k++) {
            $curr_component = $url_string_components[$k];
            if ($k >= $max_depth_if_over_length || strlen($url_short) + strlen($curr_component) > $max_url_length) {
                if ($k == 0 && strlen($url_short) < $max_url_length) {
                    // Always show a portion of first directory
                    $url_short .= substr($curr_component, 0, $max_url_length - strlen($url_short));
                }
                $url_short .= $ellipsis;
                break;
            }
            $url_short .= $curr_component;
        }

    } else {
        $url_short = $url_full;
    }

    return "<a rel=\"nofollow\" href=\"$url_full\">$url_short</a>";
}

    private static function Remove($text)
    {
        $c = 16;
        while ( $c > 0 )
        {
            $c--;
            $text = str_replace(chr(3) . strval($c), "", $text);
            if ( $c < 10 )
            {
                $text = str_replace(chr(3) . "0" . strval($c), "", $text);
            }
        }
        $text = str_replace(chr(2), "", $text);
        $text = str_replace(chr(3), "", $text);
        return str_replace(chr(1), "", $text); 
    }

    private static function RenderLogs($logs)
    {
        echo ( "<div class=logs><table>\n" );
        foreach ($logs as $blah)
        {
            if ( $blah["act"] == 1 )
            {
            echo ( "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>* <b>" . $blah["nick"] . "</b> " . self::auto_link_text(htmlspecialchars(self::Remove($blah["contents"]))) . "</td></tr>\n" );
                continue;
            }
            echo ( "<tr><td width=120><b>" . $blah["time"] . "</b></td><td><b>&lt;" . $blah["nick"] . "&gt;</b></td><td>" . self::auto_link_text(htmlspecialchars(self::Remove($blah["contents"]))) . "</td></tr>\n" );
        }
        echo ( "</table></div>\n" );
    }

    private static function Render2($logs)
    {
        echo ( "<div class=logs><p>\n" );
        foreach ($logs as $blah)
        {
            echo ( "'''" . $blah["time"] . " [" . $blah["nick"] . "]''' " . htmlspecialchars ( self::Remove( $blah["contents"] ) ) . "&lt;br&gt;<br>\n" );
        }
        echo ( "</p></div>\n" );
    }

    private static function GetChannels()
    {
        $list = array();
        $query = mysql_query("SELECT DISTINCT(channel) FROM logs");
        while($item = mysql_fetch_assoc($query))
        {
            $list[] = $item["channel"];
        }
        return $list;
    }

    private static function RenderMenu()
    {
        echo ("    <b>Channels:</b><br>\n<ul>\n");
        $channels = self::GetChannels();
        foreach ($channels as $channel)
        {
            echo ("<li><a href=\"index.php?display=" . urlencode($channel) . "\">$channel</a></li>\n");
        }
        echo ( "</ul>" );
    }

    private static function RenderIndex()
    {
        echo("    <p>This is a wikimedia logs browser, please pick a channel from menu on left side. This page is open source, if you don't like it, please fix it instead of complains!</p>");
    }

    private static function RenderChan()
    {
        echo ("<h2>" . self::$channel . "</h2>\n");
        echo ("<form><table>\n<tr>\n<td colspan=2>Filter</td></tr>");
        $StartDate = date("m/d/Y");
        if (isset($_GET["start"]))
        {
            $StartDate = $_GET["start"];
        }
        $FinishingDate = date("m/d/Y");
        if (isset($_GET["end"]))
        {
            $FinishingDate = $_GET["end"];
        }
        echo ("<tr><td>Start date</td><td><input id=\"datepicker\" value=\"$StartDate\" name=\"start\"></td></tr>\n");
        echo ("<tr><td>End date</td><td><input id=\"datepicker2\" value=\"" . $FinishingDate . "\" name=\"end\"></td></tr>\n");
        $checked = "";
        if (isset ($_GET['wiki'] ) )
        {
            $checked = "checked=on";
        }
        echo ("<tr><td></td><td><input $checked type=\"checkbox\" value=\"true\" name=\"wiki\">Convert to wiki text</td></tr>\n");
        echo ("<tr><td><input type=\"submit\" value=\"Display\"></td><td></td></tr></table>\n");
        echo ("<input type=\"hidden\" name=\"display\" value=\"" . self::$channel . "\">");
        echo ("</form>");
        if (isset($_GET["start"]) && isset($_GET["end"]))
        {
            echo ( "<hr>" );
            $c = 0;
            $logs = array();
            $start = 
            $sql="SELECT * FROM logs WHERE channel = '" . mysql_escape_string (self::$channel ) . "' and time > str_to_date( '" . mysql_escape_string($_GET["start"] . " 00:00:02"). "', '%m/%d/%Y %H:%i:%s' ) and time < str_to_date( '" . mysql_escape_string($_GET["end"] . " 23:59:59") . "', '%m/%d/%Y %H:%i:%s' );";
            echo ( "\n\n<!-- $sql -->\n\n" );
            $query = mysql_query( $sql );
            if (!$query) 
            {
                echo('Debug SQL failure: ' . mysql_error());
            }
            while($item = mysql_fetch_assoc($query))
            {
                $logs[] = $item;
                $c++;
            }
            if ( $c == 0 )
            {
                echo ( "No logs found, try a different filter" );
                return;
            }
            echo ( "<p>Displaying $c items:</p>" );
            if (isset( $_GET["wiki"]))
            {
                self::Render2($logs);
                return;
            }
            self::RenderLogs($logs);
        }
    }

    private static function RenderContent()
    {
        switch (self::$page)
        {
            case "find":
                return;
            case "display":
                self::RenderChan();
                return;
            case "index":
                self::RenderIndex();
                return;
        }
    }

    public static function Render()
    {
        echo ("<!DOCTYPE html>\n<HTML>\n<head>\n  <title>" . self::$title ."</title>\n");
        echo ("  <meta charset=\"ISO-8859-2\">\n");
	echo ("  <link rel=\"stylesheet\" type=\"text/css\" href=\"./style/style.css\">\n");
	//echo ("  <link href=\"./js/jquery-ui.css\" rel=\"stylesheet\" type=\"text/css\"/>\n");
    	//echo ("  <script src=\"http://ajax.googleapis.com/ajax/libs/jquery/1.5/jquery.min.js\"></script>\n");
        echo ("  <link rel=\"stylesheet\" href=\"http://code.jquery.com/ui/1.10.3/themes/smoothness/jquery-ui.css\" />\n");
        echo ("  <script src=\"http://code.jquery.com/jquery-1.9.1.js\"></script>\n  <script src=\"http://code.jquery.com/ui/1.10.3/jquery-ui.js\"></script>\n");
        echo (" <script> \n\$(function() {\n\$( \"#datepicker\" ).datepicker();\n });\n </script>");
        echo (" <script> \n\$(function() {\n\$( \"#datepicker2\" ).datepicker();\n });\n </script>");
    	//echo ("  <script src=\"http://ajax.googleapis.com/ajax/libs/jqueryui/1.8/jquery-ui.min.js\"></script>\n");
	//echo ("  <script src=\"js/scripts.js\" type=\"text/javascript\"></script>\n");
	echo ("  <meta name=\"viewport\" content=\"width=device-width; initial-scale=1.0\">\n");
        echo ("</head>\n<body>\n<div class='bkg1'>");
        echo ("<table>\n  <tr>    <td valign=\"top\" width=200>\n"); 
        self::RenderMenu();
        echo ("    </td>\n    <td " . 'valign="top">');
        self::RenderContent();
        echo ("    </td>\n  </tr>\n</table>\n");
        echo ("<p align=center>Licensed under WTF PL. This site is courtesy of wm-bot.</p>");
        echo ("</div>\n</body>\n</HTML>");
        mysql_close(self::$mysql);
    }
}
