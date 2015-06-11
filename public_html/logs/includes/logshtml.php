<?php

require( "includes/logswiki.php" );
require( "../IRC2HTML/src/me/contex/functions.php" );

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
       return parseToHTML($string);
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
