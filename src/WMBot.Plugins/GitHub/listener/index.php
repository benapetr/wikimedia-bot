<?

require ("db.php");
require ("irc.php");
require ("github.php");

$input = file_get_contents('php://input');
if (empty($input))
    die("This is a webhook for github. You can only use it as a webhook, displaying in a browser is not supported. See https://meta.wikimedia.org/wiki/Wm-bot#Git_Hub");

$payload = new GitHub($input);
if (!$payload->IsKnown())
    die("Unknown payload");
// for debugging only
// file_put_contents("/tmp/github", $entityBody);
// connect to db
$conn = new mysqli('localhost', $github_user, $github_pw);
if ($conn->connect_error)
    die("Connection failed: " . $conn->connect_error);

// get information for this repository from db
$sql = "SELECT id, name, channel, channel_token FROM wmib.github_repo_info WHERE name = '" . $payload->GetRepositoryName() . "';";
$result = mysqli_query($conn, $sql);

if (mysqli_num_rows($result) == 0)
    die("This repository " . $payload->GetRepositoryName() ." is not known by wm-bot");

$messages = $payload->GetMessage();
while ($row = mysqli_fetch_assoc($result))
{
    foreach ($messages as $message)
        IRC::DeliverMessage($message, $row["channel"], $row["channel_token"]);
}
$conn->close();
