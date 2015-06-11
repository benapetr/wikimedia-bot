<?php
ini_set('display_errors',1);
ini_set('display_startup_errors',1);
error_reporting(-1);

define('ENTRY_POINT', true);

// This is interface for wm-bot's logs
require_once ("psf/psf.php");
require_once ("includes/core.php");
require_once ("includes/logshtml.php");
require_once ("menu.php");
$exec=microtime();

// Initialize some variables
Logs::Init();
$selected_channel = null;
$recent = null;
$displaying_logs = isset($_GET['display']) && isset($_GET['start']) && isset($_GET['end']);
if (isset($_GET['display']))
     $selected_channel = $_GET['display'];

$html = new HtmlPage("Wikimedia IRC logs browser");

// Github ribbon
$github_ribbon = new GitHub_Ribbon();
$github_ribbon->Repository = "benapetr/wikimedia-bot/tree/master/public_html/logs";
$html->AppendHtml($github_ribbon->ToHtml());
$html->ExternalCss[] = "style/style.css";
$html->ExternalCss[] = "http://code.jquery.com/ui/1.10.3/themes/smoothness/jquery-ui.css";
$html->ExternalJs[] = "http://code.jquery.com/jquery-1.9.1.js";
$html->ExternalJs[] = "http://code.jquery.com/ui/1.10.3/jquery-ui.js";

// Header
$header = "Wikimedia IRC logs browser";
if ($selected_channel !== null)
    $header .= " - " . htmlspecialchars($selected_channel);
$html->AppendHtmlLine("<h1 class=header>$header</h1>");

// Create a layout for interface, we use just a simple html table with no border that contains all stuff
$layout = new HtmlTable();

if ($selected_channel === null)
{
    $page = "<p>This is a Wikimedia IRC logs browser, please pick a channel from menu on left side.</p>\n";
    $page .= "<p>This page is open source, if you don't like anything on it, please fix it instead of complaining!</p>\n";
} else
{
    $page = Generate_Picker($selected_channel);
    if ($displaying_logs)
    {
        $page .= "<hr>\n";
        $page .= FetchLogs($selected_channel);
    }
}

$layout->InsertRow(array(HtmlPage::IndentText(Generate_Menu(), 6), HtmlPage::IndentText($page, 6)));
$layout->BorderSize=0;

// We need to style the layout a bit as well
$layout->Format = "class=layout";

// load some extra javascript into page header that we need to use for picker
$html->InternalJs[] = "$(function() {\n    $( \"#datepicker\" ).datepicker();\n});";
$html->InternalJs[] = "$(function() {\n    $( \"#datepicker2\" ).datepicker();\n});";
$html->AppendHtml($layout->ToHtml());
$html->AppendHtml("<p>This page is generated from SQL logs, you can also download static txt files from <a href=http://wm-bot.wmflabs.org/logs>here</a></p>");
$html->PrintHtml();

$et = microtime() - $exec;
echo ("<!-- finished in $et seconds -->");
