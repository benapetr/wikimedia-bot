<?

class IRC
{
    static public function DeliverMessage($text, $channel, $token = "")
    {
        $fp = fsockopen("tcp://127.0.0.1", 64834, $errno, $errstr);
        if (!$fp)
        {
            echo "ERROR: $errno - $errstr<br />\n";
        } else
        {
            if ($token == "")
                fwrite($fp, $channel . " " . $text);
            else
                fwrite($fp, $channel . " " . $token . " " . $text);
            fclose($fp);
        }
        return true;
    }
}
