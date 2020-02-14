<?php
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or   
//  (at your option) version 3.                                         

//  This program is distributed in the hope that it will be useful,     
//  but WITHOUT ANY WARRANTY; without even the implied warranty of      
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the       
//  GNU General Public License for more details.                        

//  You should have received a copy of the GNU General Public License   
//  along with this program; if not, write to the                       
//  Free Software Foundation, Inc.,                                     
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.


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
$conn = new mysqli($github_db_host, $github_user, $github_pw);
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
