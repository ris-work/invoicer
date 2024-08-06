--Decaminute latency averages (worst case 5 per dest)
WITH candidates AS (SELECT DISTINCT(dest) AS dest FROM pings WHERE dest LIKE '192%'), 
stats_decaminutes AS (SELECT substr(time_now, 1, 15) AS decaminute, AVG(latency) AS latency_average, count(latency) AS ping_counts, 
    dest, SUM(was_it_ok_not_corrupt)/COUNT(was_it_ok_not_corrupt) AS success_rate 
    FROM pings GROUP BY decaminute, dest),
limited_to_5_worst AS (SELECT *, row_number() OVER (PARTITION BY dest ORDER BY latency_average DESC) AS group_rn FROM stats_decaminutes)
SELECT * FROM candidates can JOIN limited_to_5_worst sd ON can.dest = sd.dest WHERE ping_counts > 10 AND group_rn < 5 ORDER BY latency_average DESC;

--Success rates per decaminute
WITH candidates AS (SELECT DISTINCT(dest) AS dest FROM pings WHERE dest LIKE '192%'), stats_decaminutes AS (SELECT substr(time_now, 1, 15) AS decaminute, AVG(latency) AS latency_average, count(latency) AS ping_counts, dest, SUM(was_it_ok_not_corrupt)/COUNT(was_it_ok_not_corrupt) AS success_rate FROM pings GROUP BY decaminute, dest) SELECT * FROM candidates can JOIN stats_decaminutes sd ON can.dest = sd.dest WHERE ping_counts > 10 ORDER BY success_rate LIMIT 50;
WITH candidates AS (SELECT DISTINCT(dest) AS dest FROM pings WHERE dest LIKE '192%'), 
stats_decaminutes AS (SELECT substr(time_now, 1, 15) AS decaminute, AVG(latency) AS latency_average, count(latency) AS ping_counts, 
    dest, 100*SUM(was_it_ok_not_corrupt)/COUNT(was_it_ok_not_corrupt) AS success_rate 
    FROM pings GROUP BY decaminute, dest),
limited_to_5_worst AS (SELECT *, row_number() OVER (PARTITION BY dest ORDER BY success_rate) AS group_rn FROM stats_decaminutes)
SELECT * FROM candidates can JOIN limited_to_5_worst sd ON can.dest = sd.dest WHERE ping_counts > 10 AND group_rn < 5 ORDER BY success_rate;

--Decaminute latency averages (worst 5 per dest, all destinations)
WITH candidates AS (SELECT DISTINCT(dest) AS dest FROM pings), 
stats_decaminutes AS (SELECT substr(time_now, 1, 15) AS decaminute, AVG(latency) AS latency_average, count(latency) AS ping_counts, 
    dest, 100*SUM(was_it_ok_not_corrupt)/COUNT(was_it_ok_not_corrupt) AS success_rate 
    FROM pings GROUP BY decaminute, dest),
limited_to_5_worst AS (SELECT *, row_number() OVER (PARTITION BY dest ORDER BY latency_average DESC) AS group_rn FROM stats_decaminutes)
SELECT * FROM candidates can JOIN limited_to_5_worst sd ON can.dest = sd.dest WHERE ping_counts > 10 AND group_rn < 5 ORDER BY latency_average DESC;