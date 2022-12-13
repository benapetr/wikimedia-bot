<?php

class LogsWiki
{
    public static function Render2($logs)
    {
        $html = "<div class=logs><p>\n";
        foreach ($logs as $blah)
        {
            if ( $blah["type"] == 0 )
            {
                if ( $blah["act"] == 0 )
                {
                    $html .= "'''&lt;nowiki&gt;" . $blah["time"] . " [" . $blah["nick"] . "]&lt;/nowiki&gt;''' &lt;nowiki&gt;" . 
                       htmlspecialchars ( LogsHtml::Remove( $blah["contents"] ) ) . "&lt;/nowiki&gt;&lt;br&gt;<br>\n";
                } else
                {
                    $html .= "'''" . $blah["time"] . " --" . $blah["nick"] . "''' &lt;nowiki&gt;" . 
                       htmlspecialchars ( LogsHtml::Remove( $blah["contents"] ) ) . "&lt;/nowiki&gt;&lt;br&gt;<br>\n";
                }
            }
            switch ( $blah["type"] )
            {
                case 1:
                    $html .= "'''" . $blah["time"] . " =" . $blah["nick"] . " has quit:''' " . htmlspecialchars ( LogsHtml::Remove( $blah["contents"] ) ) . "&lt;br&gt;<br>\n";
                    break;
                case 2:
                    $html .= "'''" . $blah["time"] . " =" . $blah["nick"] . " joined channel'''" . "&lt;br&gt;<br>\n";
                    break;
                case 3:
                    $html .= "'''" . $blah["time"] . " =" . $blah["nick"] . " parted the channel''' " . LogsHtml::al( $blah["contents"] ) . "&lt;br&gt;<br>\n";
                    break;
                case 4:
                    $html .= "'''" . $blah["time"] . " =" . $blah["nick"] . " was kicked from channel''' by " . $blah["contents"] . "&lt;br&gt;<br>\n";
                    break;
            }
        }
        $html .= "</p></div>\n";
        return $html;
    }
}
