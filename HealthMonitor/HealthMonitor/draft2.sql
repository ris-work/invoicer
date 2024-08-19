ALTER TABLE process_history ADD COLUMN main_module_path TEXT;
ALTER TABLE process_history ADD COLUMN main_module_version TEXT;

CREATE VIEW stats_decaminute_main_module_path AS
SELECT decaminute, main_module_path, avg_working_set, max_working_set_for_one_instance,
(CASE WHEN total_time-prev_total_time > 0 THEN total_time-prev_total_time ELSE 0 END) AS cpu_diff,
(CASE WHEN total_time-prev_total_time > 0 THEN total_time-prev_total_time ELSE 0 END)*
100.0/(1000 * (unixepoch(decaminute || '0') - unixepoch(prev_decaminute || '0')))  AS cpu_percent,
unixepoch(decaminute || '0') - unixepoch(prev_decaminute || '0') AS time_diff,
thread_count FROM (
SELECT substring(time_now, 1, 15) AS decaminute, main_module_path, sum(working_set)/tcbdm.count AS avg_working_set,
max(working_set) AS max_working_set_for_one_instance,
sum(total_time)/tcbdm.count AS total_time,
lag(sum(total_time)/tcbdm.count) OVER (PARTITION BY main_module_path ORDER BY time_now) AS prev_total_time,
lag(substring(time_now, 1, 15)) OVER (PARTITION BY main_module_path ORDER BY time_now) AS prev_decaminute,
avg(thread_count) AS thread_count
FROM process_history ph JOIN times_collected_by_decaminute tcbdm ON decaminute=tcbdm.time_decaminute
GROUP BY substring(time_now, 1, 15), main_module_path
);

CREATE VIEW stats_hourly_main_module_path AS
SELECT hour, main_module_path, avg_working_set, max_working_set_for_one_instance,
(CASE WHEN total_time-prev_total_time > 0 THEN total_time-prev_total_time ELSE 0 END) AS cpu_diff,
(CASE WHEN total_time-prev_total_time > 0 THEN total_time-prev_total_time ELSE 0 END)*
100.0/(1000 * (unixepoch(hour || ':00') - unixepoch(prev_hour || ':00')))  AS cpu_percent,
unixepoch(hour || ':00') - unixepoch(prev_hour || ':00') AS time_diff,
thread_count FROM (
SELECT substring(time_now, 1, 13) AS hour, main_module_path, sum(working_set)/tcbh.count AS avg_working_set,
max(working_set) AS max_working_set_for_one_instance,
sum(total_time)/tcbh.count AS total_time,
lag(sum(total_time)/tcbh.count) OVER (PARTITION BY main_module_path ORDER BY time_now) AS prev_total_time,
lag(substring(time_now, 1, 13)) OVER (PARTITION BY main_module_path ORDER BY time_now) AS prev_hour,
avg(thread_count) AS thread_count
FROM process_history ph JOIN times_collected_by_hour tcbh ON hour=tcbh.time_hour
GROUP BY substring(time_now, 1, 13), main_module_path
);