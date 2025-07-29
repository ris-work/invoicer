DELETE FROM public.inventory_movements;
-- Seed public.inventory_movements with 500 random rows
WITH base AS (

  -- Generate 6 item/batch combos × 100 series = 600 candidates
  SELECT
    c.itemcode,
    c.batchcode,

    (random() < 0.9)::boolean                                 AS batch_enabled,
    now() - random() * interval '180 days'                     AS mfg_date,
    now() + ((30 + (random() * 365))::int) * interval '1 day' AS exp_date,
    round((1 + random() * 9)::numeric, 2)::real                AS packed_size,

    p.prefix,
    p.code,

    -- negative for definitive deductions, random sign for adjustments
    (CASE
       WHEN p.prefix IN (
         'sale', 'consumption', 'issue', 'purchasereturn', 'deprecated'
       )
       THEN -round((random() * 100)::numeric, 2)::double precision

       WHEN p.prefix = 'adjustment'
       THEN CASE
              WHEN random() < 0.5
              THEN  round((random() * 100)::numeric, 2)::double precision
              ELSE -round((random() * 100)::numeric, 2)::double precision
            END

       ELSE round((random() * 100)::numeric, 2)::double precision
     END)                                                      AS units,

    'qty'::text                                               AS measurement_unit,

    -- Random prices within realistic bands
    round((50 + random() * 100)::numeric, 2)::double precision AS marked_price,
    round((30 + random() * 80)::numeric, 2)::double precision  AS selling_price,
    round((10 + random() * 50)::numeric, 2)::double precision  AS cost_price,

    (random() < 0.3)::boolean                                  AS volume_discounts,
    (1 + floor(random() * 5))::bigint                          AS suppliercode,
    (random() < 0.3)::boolean                                  AS user_discounts,

    now() - random() * interval '90 days'                      AS last_counted_at,
    substr(md5(random()::text), 1, 10)::text                  AS remarks,
    (p.prefix || ':' || p.code)                               AS reference,
    now() - random() * interval '90 days'                      AS entered_time,
    random()                                                  AS rnd

  FROM
    (VALUES (1,1),(1,2),(2,1),(2,2),(3,1),(3,2)) AS c(itemcode, batchcode)

  CROSS JOIN generate_series(1,100)

  CROSS JOIN LATERAL (
    SELECT
      (ARRAY[
         'sale','consumption','issue',
         'production','salesreturn','purchase',
         'purchasereturn','adjustment','deprecated'
       ])[floor(random() * 9 + 1)]                              AS prefix,
      lpad((floor(random() * 9000 + 1000)::int)::text, 4, '0')  AS code
  ) AS p
)

INSERT INTO public.inventory_movements (
  itemcode, batchcode, batch_enabled,
  mfg_date, exp_date, packed_size,
  units, measurement_unit,
  marked_price, selling_price, cost_price,
  volume_discounts, suppliercode, user_discounts,
  last_counted_at, remarks, reference, entered_time
)
SELECT
  itemcode, batchcode, batch_enabled,
  mfg_date, exp_date, packed_size,
  units, measurement_unit,
  marked_price, selling_price, cost_price,
  volume_discounts, suppliercode, user_discounts,
  last_counted_at, remarks, reference, entered_time
FROM base
ORDER BY rnd
LIMIT 500;
