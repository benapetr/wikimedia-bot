-- DROP table test.github_repo_info;

CREATE TABLE wmib.github_repo_info
(
    id int AUTO_INCREMENT,
    name varchar(80),
    channel varchar(80),
    channel_token varchar(80),
    push bool,
    primary key(id)
);

GRANT select on wmib.github_repo_info to github@'localhost';
