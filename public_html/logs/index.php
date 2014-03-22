<?

// This is interface for wm-bot's logs
require ("includes/core.php");
$exec=microtime();

Logs::Init();
Logs::Render();

$et = microtime() - $exec;
echo ("<!-- finished in $et seconds -->");
