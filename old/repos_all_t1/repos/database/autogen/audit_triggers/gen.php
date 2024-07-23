#!/usr/bin/php
<?php
echo "gen.php [SQLSCRIPT]\n";
echo "Generates CUD audit triggers (not on reads).\n";
echo "output (is appeneded): [SQLSCRIPT].audit.sql\n";

if($argv[1]){
	$sql_orig=file_get_contents($argv[1]);
	preg_match("/DROP TABLE IF EXISTS (.*)\\.(.*);/", $sql_orig, $sql_fulltablename);
//	var_dump($sql_orig);
//	var_dump($sql_fulltablename);
	$schemaname=$sql_fulltablename[1];
	$tablename=$sql_fulltablename[2];
	$schema_table=$sql_fulltablename[1].".".$sql_fulltablename[2];
	var_dump($schema_table);
	//SPLIT COLDEFS
	$col_lookup="/CREATE TABLE IF NOT EXISTS ".$schema_table."\n\\((.*)\\)\nWITH/ms";
	//echo $col_lookup;
	preg_match($col_lookup, $sql_orig, $sql_coldefs_out);
	$sql_coldefs=$sql_coldefs_out[1];
	var_dump($sql_coldefs);
	$trigger_function="
-- FUNCTION: public.{$tablename}.version_force()

-- DROP FUNCTION IF EXISTS public.{$tablename}.version_force();

CREATE OR REPLACE FUNCTION public.{$tablename}.version_force()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS \$BODY\$
BEGIN
IF (NEW.version != 0) AND (NEW.version IS NOT NULL) THEN
RAISE EXCEPTION 'AUDIT EXCEPTION. VERSION SHOULD ALWAYS BE ZERO';
END IF;
IF NEW.id IS NULL THEN
RAISE EXCEPTION 'ID CAN NOT BE NULL.';
END IF;
NEW.db_time:=now();
NEW.version:=0;

UPDATE {$tablename}.audit SET version=version-1 WHERE id=NEW.id;
IF (TG_OP='UPDATE') THEN
INSERT INTO {$tablename}_audit values (NEW.*, 0,TG_OP,now());
ELSE IF (TG_OP='INSERT') THEN
INSERT INTO {$tablename}_audit values (NEW.*, 0,TG_OP,now());
ELSE IF (TG_OP='DELETE') THEN
INSERT INTO {$tablename}_audit values (OLD.*, 0,TG_OP,now());
END IF;
RETURN NEW;
END;
\$BODY\$;

CREATE FUNCTION IF NOT EXISTS public.no_deletes()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS \$BODY\$
BEGIN
IF (TG_OP='DELETE') THEN
RAISE EXCEPTION 'YOU MAY NOT DELETE ANY ENTRY IN AN AUDIT TABLE';
END;
\$BODY\$;
ALTER FUNCTION public.{$tablename}.version_force()
    OWNER TO postgres;
ALTER FUNCTION public.no_deletes()
    OWNER TO postgres;

\n";
 	echo $trigger_function;
	file_put_contents($argv[1].".trigger.function.sql", $trigger_function, FILE_APPEND | LOCK_EX);
	$create_trigger="
CREATE TRIGGER {$tablename}_audit_trigger BEFORE INSERT OR UPDATE OR DELETE ON $schema_table
FOR EACH ROW EXECUTE FUNCTION
public_{$tablename}_version_force();
";
	echo $create_trigger;
	file_put_contents($argv[1].".trigger.sql", $create_trigger, FILE_APPEND | LOCK_EX);
	$sql_noprimarykey=str_ireplace('PRIMARY KEY', '', $sql_orig);
	echo $sql_noprimarykey;
	$sql_noprimarykey_lines=explode("\n", $sql_noprimarykey);
	var_dump($sql_noprimarykey_lines);
	echo(count($sql_noprimarykey_lines)."\n");
	//REMOVE CONSTRAINTS
	for($i=0; $i<count($sql_noprimarykey_lines) ; $i++ )
		if (strstr(strtolower($sql_noprimarykey_lines[$i]), "constraint")) $sql_noprimarykey_lines[$i]="";
	for($i=0; $i<count($sql_noprimarykey_lines) ; $i++ )
		if (strstr(strtolower($sql_noprimarykey_lines[$i]), "(")) {$offset=$i; break;}
	echo  $offset . "\n";
	var_dump($sql_noprimarykey_lines);
}
?>
