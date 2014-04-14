create table logs
(
  id int AUTO_INCREMENT,
  channel varchar(200),
  nick varchar(200),
  time datetime,
  act bool,
  contents text,
  type int,
  host varchar(80),
  primary key (id)
);
