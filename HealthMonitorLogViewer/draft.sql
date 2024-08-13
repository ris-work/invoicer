CREATE VIEW stats_decaminute AS
SELECT substring(time_now, 1, 15) AS decaminute, process_name, avg(working_set)*count(process_name)/tcbdm.count AS avg_working_set,
max(working_set) AS max_working_set_for_one_instance,
max(total_time) AS total_time,
avg(thread_count) AS thread_count
FROM process_history ph JOIN times_collected_by_decaminute tcbdm ON decaminute=tcbdm.time_decaminute 
GROUP BY substring(time_now, 1, 15), process_name;

CREATE VIEW stats_hourly AS 
SELECT substring(time_now, 1, 13) AS hour, process_name, avg(working_set)*count(process_name)/tcbh.count AS avg_working_set,
max(working_set) AS max_working_set_for_one_instance,
max(total_time) AS total_time,
avg(thread_count) AS thread_count
FROM process_history ph JOIN times_collected_by_hour tcbh ON hour=tcbh.time_hour 
GROUP BY substring(time_now, 1, 13), process_name;