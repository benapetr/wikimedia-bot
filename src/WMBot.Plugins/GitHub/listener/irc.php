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
