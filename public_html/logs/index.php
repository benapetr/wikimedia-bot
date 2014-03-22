<?

// This is interface for wm-bot's logs
require ("includes/core.php");
$exec=time();

Logs::Init();
Logs::Render();

echo "<!-- finished in " . time() - $exec . " seconds -->";
