CREATE VIEW stats_decaminute AS
SELECT decaminute, process_name, avg_working_set, max_working_set_for_one_instance, 
(CASE WHEN total_time_avg-prev_total_time_avg > 0 THEN total_time_avg-prev_total_time_avg ELSE 0 END) AS cpu_diff,
unixepoch(decaminute || '0') - unixepoch(prev_decaminute || '0') AS time_diff,
thread_count FROM (
SELECT substring(time_now, 1, 15) AS decaminute, process_name, avg(working_set)*count(process_name)/tcbdm.count AS avg_working_set,
max(working_set) AS max_working_set_for_one_instance,
avg(total_time)*count(process_name) AS total_time_avg,
lag(avg(total_time)*count(process_name)) OVER (PARTITION BY process_name ORDER BY time_now) AS prev_total_time_avg,
lag(substring(time_now, 1, 13)) OVER (PARTITION BY process_name ORDER BY time_now) AS prev_decaminute,
avg(thread_count) AS thread_count
FROM process_history ph JOIN times_collected_by_decaminute tcbdm ON decaminute=tcbdm.time_decaminute 
GROUP BY substring(time_now, 1, 15), process_name
);

CREATE VIEW stats_hourly AS 
SELECT hour, process_name, avg_working_set, max_working_set_for_one_instance, 
(CASE WHEN total_time_avg-prev_total_time_avg > 0 THEN total_time_avg-prev_total_time_avg ELSE 0 END) AS cpu_diff,
unixepoch(hour || ':00') - unixepoch(prev_hour || ':00') AS time_diff,
thread_count FROM (
SELECT substring(time_now, 1, 13) AS hour, process_name, avg(working_set)*count(process_name)/tcbh.count AS avg_working_set,
max(working_set) AS max_working_set_for_one_instance,
avg(total_time)*count(process_name) AS total_time_avg,
lag(avg(total_time)*count(process_name)) OVER (PARTITION BY process_name ORDER BY time_now) AS prev_total_time_avg,
lag(substring(time_now, 1, 13)) OVER (PARTITION BY process_name ORDER BY time_now) AS prev_hour,
avg(thread_count) AS thread_count
FROM process_history ph JOIN times_collected_by_hour tcbh ON hour=tcbh.time_hour 
GROUP BY substring(time_now, 1, 13), process_name
);