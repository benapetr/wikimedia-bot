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
        $this->commit_user = $data["author"]["username"];
        $this->commit_author = $data["author"]["name"];
        $this->commit_author_mail = $data["author"]["email"];
    } 
}

class GitHub
{
    private $priv_json = array();
    private $payload_type;
    private $repo_name;
    private $commits = array();
    private $messages = array();

    function __construct($json)
    {
        $this->payload_type = 0;
        $this->priv_json = json_decode($json, true);
        if ($this->priv_json === null)
            die("Unable to decode: " . $json);
        //var_dump($this->priv_json);
        $this->repo_name = $this->priv_json['repository']['full_name'];
        if (array_key_exists("commits", $this->priv_json))
            $this->payload_type = 1;
        $this->process_msgs();
    }

    private function getHeader()
    {
        return chr(2) . "GitHub".chr(2)." [" . chr(3) . "8" . $this->repo_name . chr(3) . "] ";
    }

    private function process_msgs()
    {
        if ($this->payload_type == 1)
        {
            $this->commits = $this->pCommitInfo();
            if (array_key_exists("pusher", $this->priv_json))
                array_push($this->messages, $this->getHeader() . chr(2) . $this->priv_json["pusher"]["name"] . chr(2) . " pushed " . count($this->commits) . " commits: " . $this->priv_json["compare"]);

            foreach ($this->commits as $commit)
            {
               $message = $this->getHeader() . "commit by " . chr(2) . $commit->commit_user . " (" . $commit->commit_author . ")" . chr(2) . " " . $commit->github . " " . $commit->commit_message; 
               array_push($this->messages, $message);
            }
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




