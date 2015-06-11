<?php

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

        for ($i = 0; $i < count($c); $i++)
        {
            $n1 = 0;
            $n2 = 0;
            $n3 = 0;
            $x[$i] = preg_replace("/\x03(\d\d?),(\d\d?)(.*?)(?(?=\x03)|$)/e", "'<span style=\"color: #'.\$c['$1'].';\">$3</span>'", $x[$i], -1, $n1);
            $x[$i] = preg_replace("/\x03(\d\d?)(.*?)(?(?=\x03)|$)/e", "'<span style=\"color: #'.\$c['$1'].';\">$2</span>'", $x[$i], -1, $n2);
            $x[$i] = preg_replace("/\x03|\x0F/", "<span style=\"color: #000;\">", $x[$i], -1, $n3);
            $x[$i] = preg_replace("/\x02(.*?)((?=\x02)\x02|$)/", "<b>$1</b>", $x[$i]);
            $x[$i] = preg_replace("/\x1F(.*?)((?=\x1F)\x1F|$)/", "<u>$1</u>", $x[$i]);
            $y = $n1 + $n2 + $n3;
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

    public static function RenderLogs($logs)
    {
        $html = "<div class=logs><table class=logs>\n";
        foreach ($logs as $blah)
        {
            if ( $blah["type"] == 0 )
            {
		    if ( $blah["act"] == 1 )
		    {
		        $html .= "<tr><td width=120><b>" . $blah["time"] .
			   "</b></td><td colspan=2>* <b>" . 
			   $blah["nick"] . "</b> " . self::al($blah["contents"]) . 
			   "</td></tr>\n";
			continue;
		    }
	        $html .= ( "<tr><td width=120><b>" . $blah["time"] . 
			   "</b></td><td><b>&lt;" . $blah["nick"] . 
			   "&gt;</b></td><td>" . self::al($blah["contents"]) . "</td></tr>\n" );
                continue;
            }
            switch ( $blah["type"] )
            {
                case 1:
                    $html .= "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["nick"] . " has quit: " . self::al( $blah["contents"] ) . "</td></tr>\n";
                    continue;
                case 2:
                    $html .= "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["nick"] . " joined channel</td></tr>\n";
                    continue;
                case 3:
                    $html .= "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["nick"] . " parted the channel " . self::al( $blah["contents"] ) . "</td></tr>\n";
                    continue;
                case 4:
                    $html .= "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["nick"] . " was kicked from channel by " . $blah["contents"] . "</td></tr>\n";
                    continue;
                case 6:
                    $html .= "<tr><td width=120><b>" . $blah["time"] . "</b></td><td colspan=2>** " . $blah["contents"] . " changed nickname to " . $blah["nick"] . "</td></tr>\n";
                    continue;
            }
        }
        $html .= "</table></div>\n";
        return $html;
    }
}
