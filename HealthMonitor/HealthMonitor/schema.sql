CREATE TABLE repository.pings (dest TEXT, time_now TEXT, latency INT, corrupt INT, PRIMARY KEY (dest, time_now)) STRICT, WITHOUT ROWID;
CREATE TABLE repository.exceptions(time_now TEXT, exception TEXT);
CREATE TABLE repository.process_history (time_now TEXT, pid INT, process_name TEXT, thread_count INT, virtual_memory_use TEXT, paged_memory_use TEXT, private_memory_use TEXT, working_set TEXT, main_window_title TEXT, started TEXT, system_time INT,
user_time INT,
total_time INT, PRIMARY KEY (time_now, pid)) STRICT, WITHOUT ROWID;
CREATE TABLE localdb.vvar(
  name TEXT PRIMARY KEY NOT NULL,
  value CLOB,
  CHECK( typeof(name)='text' AND length(name)>=1 )
);
CREATE TABLE localdb.vfile(
  id INTEGER PRIMARY KEY,
  vid INTEGER REFERENCES blob,
  chnged INT DEFAULT 0,
  deleted BOOLEAN DEFAULT 0,
  isexe BOOLEAN,
  islink BOOLEAN,
  rid INTEGER,
  mrid INTEGER,
  mtime INTEGER,
  pathname TEXT,
  origname TEXT,
  mhash TEXT,
  UNIQUE(pathname,vid)
);
CREATE TABLE localdb.vmerge(
  id INTEGER REFERENCES vfile,
  merge INTEGER,
  mhash TEXT
);
CREATE TABLE localdb.sqlite_stat1(tbl,idx,stat);
CREATE UNIQUE INDEX localdb.vmergex1 ON vmerge(id,mhash);
CREATE INDEX localdb.vfile_nocase  ON vfile(pathname COLLATE nocase);
CREATE TRIGGER localdb.vmerge_ck1 AFTER INSERT ON vmerge
WHEN new.mhash IS NULL BEGIN
  SELECT raise(FAIL,
  'trying to update a newer check-out with an older version of Fossil');
END;
CREATE TABLE configdb.global_config(
  name TEXT PRIMARY KEY,
  value TEXT
);
CREATE TABLE configdb.sqlite_stat1(tbl,idx,stat);
