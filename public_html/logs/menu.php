<?php

if (!defined("ENTRY_POINT"))
    die("This is not a valid entry point");

function Generate_Menu()
{
    global $recent;
    $html = "<div class=menu>\n<b>Channels:</b><br>\n";
    $channels = Logs::GetChannels();
    if ($recent)
        $html .= "<a href=?recent=disable>Display all</a>\n";
    else
        $html .= "<a href=?recent=yes>Display only active channels</a>\n";
    $html .= "<ul>\n";
    foreach ($channels as $channel)
    {
        $name = $channel;
        if (strlen($channel) > 22)
        {
           $name = substr ( $channel, 0, 18 ) . "....";
        }
        $html .= "<li class=menu><a href=\"index.php?display=" . urlencode($channel) . "\">$name</a></li>\n";
    }
    $html .= "</ul>\n</div>";
    return $html;
}

function Generate_Picker($channel)
{
    $html = "<div class=filter>\n";
    $html .= "  <b>Filter:</b><br>\n";
    $html .= "  <form>\n";
    $StartDate = date("m/d/Y");
    if (isset($_GET["start"]))
        $StartDate = $_GET["start"];
    $FinishDate = date("m/d/Y");
    if (isset($_GET["end"]))
        $FinishDate = $_GET["end"];
    $picker = new HtmlTable();
    $picker->InsertRow(array("Start date", "<input id='datepicker' value=\"$StartDate\" name='start'>"));
    $picker->BorderSize = 0;
    $picker->InsertRow(array("End date", "<input id='datepicker2' value=\"$FinishDate\" name='end'>"));
    $show = "";
    if (isset ($_GET['data'] ) )
        $show = "checked=on ";
    $checked = "";
    if (isset ($_GET['wiki'] ) )
        $checked = "checked=on ";
    $picker->InsertRow(array("", "<label><input " . $checked . "type='checkbox' value='true' name='wiki'>Convert to wiki text</label>"));
    $picker->InsertRow(array("", "<label><input ". $show . "type='checkbox' value='true' name='data'>Show part / join / quit / kick / nick</label>"));
    $html .= psf_indent_text($picker->ToHtml(), 4);
    $html .= "<input type='submit' value='Display'><input type='hidden' name='display' value=\"$channel\"></form>\n</div>\n";
    return $html;
}

function FetchLogs($channel)
{
    $html = "";
    $c = 0;
    $logs = array();
    $display_joins = isset ($_GET['data']);
    if ($display_joins)
        $sql = "SELECT * FROM logs WHERE channel = '" . pg_escape_string ($channel ) . "' and time > to_timestamp( '" . pg_escape_string($_GET["start"] . " 00:00:00"). "', 'MM/DD/YYYY HH24:MI:SS' ) and time < to_timestamp( '" . pg_escape_string($_GET["end"] . " 23:59:59") . "', 'MM/DD/YYYY HH24:MI:SS' ) order by time asc;";
    else
        $sql = "SELECT * FROM logs WHERE channel = '" . pg_escape_string ($channel ) . "' and time > to_timestamp( '" . pg_escape_string($_GET["start"] . " 00:00:00"). "', 'MM/DD/YYYY HH24:MI:SS' ) and time < to_timestamp( '" . pg_escape_string($_GET["end"] . " 23:59:59") . "', 'MM/DD/YYYY HH24:MI:SS' ) and type = 0 order by time asc;";
    $query = pg_query( $sql );
    if (!$query)
    {
       die('SQL failure: ' . pg_last_error());
    }

    while($item = pg_fetch_assoc($query))
    {
        $logs[] = $item;
        $c++;
    }
    if ( $c == 0 )
    {
        return "No logs found, try a different filter";
    }
    $html .= "<p>Displaying $c items:</p>\n";
    if (isset( $_GET["wiki"]))
        $html .= LogsWiki::Render2($logs);
    else
        $html .= LogsHtml::RenderLogs($logs);
    return $html;
}
