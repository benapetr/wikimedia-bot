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

<?
class Git_Commit
{
    public $commit_id;
    public $commit_author;
    public $commit_author_mail;
    public $commit_user;
    public $commit_message;
    public $github;

    function __construct($data)
    {
        $this->github = $data["url"];
        $this->commit_id = $data["id"];
        $this->commit_message = $data["message"];
        $this->commit_message = str_replace("\n", " ", $this->commit_message);
        if (strlen($this->commit_message > 100))
            $this->commit_message = substr($this->commit_message, 0, 100) . "...";
        $this->commit_user = $data["author"]["username"];
        $this->commit_author = $data["author"]["name"];
        $this->commit_author_mail = $data["author"]["email"];
    } 
}

class GitHub_SenderInfo
{
    public $login;
    public $id;

    function __construct($data)
    {
        $this->login = $data['login'];
        $this->id = $data['id'];
    }
}

class GitHub
{
    private $priv_json = array();
    private $payload_type;
    private $repo_name;
    private $repo_branch = "unknown";
    private $sender = null;
    private $commits = array();
    private $messages = array();

    private static function startsWith($haystack, $needle)
    {
        // search backwards starting from haystack length characters from the end
        return $needle === "" || strrpos($haystack, $needle, -strlen($haystack)) !== FALSE;
    }

    function __construct($json)
    {
        $this->payload_type = 0;
        $this->priv_json = json_decode($json, true);
        if ($this->priv_json === null)
            die("Unable to decode: " . $json);
        if (array_key_exists("sender"))
            $this->sender = new GitHub_SenderInfo($this->priv_json['sender']);
        $this->repo_name = $this->priv_json['repository']['full_name'];
        if (array_key_exists("ref", $this->priv_json))
        {
            // get a name of branch this is about
            $branch = $this->priv_json['ref'];
            if (GitHub::startsWith($branch, "refs/heads/"))
                $branch = substr($branch, 11);
            $this->repo_branch = $branch;
        }
        $action = "unknown";
        if (array_key_exists("action", $this->priv_json))
            $action = $this->priv_json['action'];
        
        if ($action == "opened")
            $this->payload_type = 2;
        else if ($action == "created")
            $this->payload_type = 3;
        else if ($action == "closed")
            $this->payload_type = 4;
        else if (array_key_exists("commits", $this->priv_json))
            $this->payload_type = 1;
        $this->process_msgs();
    }

    private function getHeader()
    {
        return chr(2) . "GitHub".chr(2)." [" . chr(3) . "8" . $this->repo_name . chr(3) . "] ";
    }

    private function process_msgs()
    {
        switch ($this->payload_type)
        {
            case 1:
            {
                $this->commits = $this->pCommitInfo();
                if (array_key_exists("pusher", $this->priv_json))
                    array_push($this->messages, $this->getHeader() . chr(2) . $this->priv_json["pusher"]["name"]
                                                . chr(2) . " pushed " . count($this->commits)
                                                . " commits into branch " . chr(2) . $this->repo_branch
                                                . chr(2) . ": " . $this->priv_json["compare"]);

                foreach ($this->commits as $commit)
                {
                   $message = $this->getHeader() . "commit by " . chr(2) . $commit->commit_user . " (" . $commit->commit_author . ")" . chr(2)
                                 . " " . $commit->github . " " . $commit->commit_message; 
                   array_push($this->messages, $message);
                }
            }
            break;
            case 2:
            case 3:
            case 4:
            {
                if (!array_key_exists("issue", $this->priv_json))
                    throw new Exception('There is no issue blob in data received from github');
                $issue = $this->priv_json['issue'];
                if (!array_key_exists("user", $issue))
                    throw new Exception('There is no user block inside of issue provided by github');
                $url = $issue['html_url'];
                $name = $issue['title'];
                $number = $issue['number'];
                $user = $issue['user']['login'];
                if ($this->payload_type == 2)
                {
                    $message = $this->getHeader() . "new issue by " . chr(2) . $user . ": " . $name . chr(2) . " " . $url;
                    array_push($this->messages, $message);
                } else if ($this->payload_type == 3)
                {
                    if (!array_key_exists("comment", $this->priv_json))
                        throw new Exception('There is no comment blob inside of issue');
                    $comment = $this->priv_json['comment'];
                    if (!array_key_exists("user", $comment))
                        throw new Exception('There is no user blob inside of comment json');
                    $user = $comment['user']['login'];
                    $url = $comment['html_url'];
                    $message = $this->getHeader() . chr(2) . $user . (chr2) . " commented on issue " . chr(2) . $name . chr(2) . ": " . $url;
                    array_push($this->messages, $message);
                } else if ($this->payload_type == 4)
                {
                    $message = $this->getHeader() . chr(2) . $this->sender->login . (chr2) . " closed issue " . chr(2) . $name . chr(2) . ": " . $url;
                    array_push($this->messages, $message);
                }
            }
            break;
        }
    }

    public function GetRepositoryName()
    {
        return $this->repo_name;
    }

    public function IsKnown()
    {
        if ($this->payload_type == 0)
            return false;
        return true;
    }

    private function pCommitInfo()
    {
        $result = array();
        foreach ($this->priv_json["commits"] as $commit)
            array_push($result, new Git_Commit($commit));
        return $result;
    }

    public function GetMessage()
    {
        return $this->messages;
    }
}




