<?

require( "includes/logswiki.php");
class LogsHtml
{
    public static $data = " type = 0 and ";

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

        if (strlen($url_full) > $max_url_length)
        {
            $parts = parse_url($url_full);
            $url_short = $parts['scheme'] . '://' . preg_replace('/^www\./', '', $parts['host']) . '/';

            $path_components = explode('/', trim($parts['path'], '/'));
            foreach ($path_components as $dir)
            {
                $url_string_components[] = $dir . '/';
            }

            if (!empty($parts['query']))
            {
                $url_string_components[] = '?' . $parts['query'];
            }

            if (!empty($parts['fragment']))
            {
                $url_string_components[] = '#' . $parts['fragment'];
            }

            for ($k = 0; $k < count($url_string_components); $k++)
            {
                $curr_component = $url_string_components[$k];
                if ($k >= $max_depth_if_over_length || strlen($url_short) + strlen($curr_component) > $max_url_length)
                {
                    if ($k == 0 && strlen($url_short) < $max_url_length)
                    {
                        // Always show a portion of first directory
                        $url_short .= substr($curr_component, 0, $max_url_length - strlen($url_short));
                    }
                    $url_short .= $ellipsis;
                    break;
                }
                $url_short .= $curr_component;
            }
        } else
        {
            $url_short = $url_full;
        }

        return "<a rel=\"nofollow\" href=\"$url_full\">$url_short</a>";
    }

    public static function ConvertColorsToHtml($string)
    {
        $x = explode("\r\n",$string);
        $x = str_replace(chr(3) . "0", chr(3), $x);
        $c = array("FFF","000","00007F","009000","FF0000","7F0000","9F009F","FF7F00","FFFF00","00F800","00908F","00FFFF","0000FF","FF00FF","7F7F7F","CFD0CF");

        for ($i = 0; $i < count($c); $i++) {
        $n1 = 0;
        $n2 = 0;
        $n3 = 0;
           $x[$i] = preg_replace("/\x03(\d\d?),(\d\d?)(.*?)(?(?=\x03)|$)/e", "'<span style=\"color: #'.\$c['$1'].';\">$3</span>'", $x[$i], -1, $n1);
           $x[$i] = preg_replace("/\x03(\d\d?)(.*?)(?(?=\x03)|$)/e", "'<span style=\"color: #'.\$c['$1'].';\">$2</span>'", $x[$i], -1, $n2);
           $x[$i] = preg_replace("/\x03|\x0F/", "<span style=\"color: #000;\">", $x[$i], -1, $n3);
           $x[$i] = preg_replace("/\x02(.*?)((?=\x02)\x02|$)/", "<b>$1</b>", $x[$i]);
           $x[$i] = preg_replace("/\x1F(.*?)((?=\x1F)\x1F|$)/", "<u>$1</u>", $x[$i]);
           $y = $n1 + $n2 + $n3;
           //$x[$i] = str_replace("\\","",$x[$i]); 
           $x[$i] = $x[$i].str_repeat("</span>",$n3);
   }
   return implode("\r\n",$x);
    }

    public static function Remove($text)
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

    public static function al($text)
    {
        return self::auto_link_text(self::Remove(self::ConvertColorsToHtml(htmlspecialchars($text))) );
    }

    private static function RenderLogs($logs)
    {
        echo ( "<div class=logs><table>\n" );
        foreach ($logs as $blah)
        {
            if ( $blah["type"] == 0 )
            {
		    if ( $blah["act"] == 1 )
		    {
		        echo ( "<tr><td width=120><b>" . $blah["time"] .
			   "</b></td><td colspan=2>* <b>" . 
			   $blah["nick"] . "</b> " . self::al($blah["contents"]) . 
			   "</td></tr>\n" );
			continue;
		    }
	        echo ( "<tr><td width=120><b>" . $blah["time"] . 
			   "</b></td><td><b>&lt;" . $blah["nick"] . 
			   "&gt;</b></td><td>" . self::al($blah["contents"]) . "</td></tr>\n" );
                continue;
            }
            switch ( $blah["type"] )
            {
                case 1:
                    echo ( "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["nick"] . " has quit: " . self::al( $blah["contents"] ) . "</td></tr>\n" );
                    continue;
                case 2:
                    echo ( "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["nick"] . " joined channel</td></tr>\n" );
                    continue;
                case 3:
                    echo ( "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["nick"] . " parted the channel " . self::al( $blah["contents"] ) . "</td></tr>\n" );
                    continue;
                case 4:
                    echo ( "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["nick"] . " was kicked from channel by " . $blah["contents"] . "</td></tr>\n" );
                    continue;
                case 6:
                    echo ( "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["contents"] . " changed nickname to " . $blah["nick"] . "</td></tr>\n" );
                    continue;
            }
        }
        echo ( "</table></div>\n" );
    }

    private static function RenderMenu()
    {
        echo ("<b>Channels:</b><br>\n<ul>\n");
        $channels = Logs::GetChannels();
        foreach ($channels as $channel)
        {
            $name = $channel;
            if (strlen($channel) > 22)
            {
                $name = substr ( $channel, 0, 18 ) . "....";
            }
            echo ("<li class=menu><a href=\"index.php?display=" . urlencode($channel) . "\">$name</a></li>\n");
        }
        echo ( "</ul>" );
    }

    private static function RenderIndex()
    {
        echo("
        	<p>This is a Wikimedia logs browser, please pick a channel from menu on left side.</p>
        	<p>This page is open source, if you don't like it, please fix it instead of complaining!</p>
        	<p>The source code is <a href='https://github.com/benapetr/wikimedia-bot/tree/master/public_html/logs'>available on GitHub</a>.</p>
        ");
    }

    private static function RenderChan()
    {
        echo ("<b>" . Logs::$channel . "</b>\n");
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
        $show = "";
        if (isset ($_GET['data'] ) )
        {
            $show = "checked=on";
        }
        $checked = "";
        if (isset ($_GET['wiki'] ) )
        {
            $checked = "checked=on";
        }
        echo ("<tr><td></td><td><input $checked type=\"checkbox\" value=\"true\" name=\"wiki\">Convert to wiki text</td></tr>\n");
        echo ("<tr><td></td><td><input $show type=\"checkbox\" value=\"true\" name=\"data\">Show part / join / quit / kick / nick</td></tr>\n");
        echo ("<tr><td><input type=\"submit\" value=\"Display\"></td><td></td></tr></table>\n");
        echo ("<input type=\"hidden\" name=\"display\" value=\"" . Logs::$channel . "\">");
        echo ("</form>");
        if (isset($_GET["start"]) && isset($_GET["end"]))
        {
            echo ( "<hr>" );
            $c = 0;
            $logs = array();
            $sql="SELECT * FROM logs WHERE " . self::$data . " channel = '" . mysql_escape_string (Logs::$channel ) . "' and time > str_to_date( '" . mysql_escape_string($_GET["start"] . " 00:00:02"). "', '%m/%d/%Y %H:%i:%s' ) and time < str_to_date( '" . mysql_escape_string($_GET["end"] . " 23:59:59") . "', '%m/%d/%Y %H:%i:%s' );";
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
                LogsWiki::Render2($logs);
                return;
            }
            self::RenderLogs($logs);
        }
    }

    private static function RenderContent()
    {
        switch (Logs::$page)
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
        echo ("<!DOCTYPE html>\n<HTML>\n<head>\n  <title>" . Logs::$title ."</title>\n");
        echo ("  <meta charset=\"ISO-8859-2\">\n");
	echo ("  <link rel=\"stylesheet\" type=\"text/css\" href=\"./style/style.css\">\n");
        echo ("  <link rel=\"stylesheet\" href=\"http://code.jquery.com/ui/1.10.3/themes/smoothness/jquery-ui.css\" />\n");
        echo ("  <script src=\"http://code.jquery.com/jquery-1.9.1.js\">\n  </script>\n  <script src=\"http://code.jquery.com/ui/1.10.3/jquery-ui.js\">\n  </script>\n");
        echo (" <script> \n\$(function() {\n\$( \"#datepicker\" ).datepicker();\n });\n </script>");
        echo (" <script> \n\$(function() {\n\$( \"#datepicker2\" ).datepicker();\n });\n </script>");
	echo ("  <meta name=\"viewport\" content=\"width=device-width; initial-scale=1.0\">\n");
        echo ("</head>\n<body>\n<div class='bkg1'>\n");
        echo ("<table>\n  <tr>\n    <td valign=\"top\" width=200>\n"); 
        self::RenderMenu();
        echo ("    </td>\n    <td " . 'valign="top">');
        self::RenderContent();
        echo ("    </td>\n  </tr>\n</table>\n");
        echo ("<p align=center>Licensed under BSD license. This site is courtesy of wm-bot.</p>");
        echo ("</div>\n</body>\n</HTML>");
    }
}
