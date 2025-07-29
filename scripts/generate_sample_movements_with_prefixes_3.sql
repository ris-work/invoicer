-- 1) Delete old rows
DELETE FROM public.inventory_movements;


-- 2) Generate 500 randomized movements
WITH 
  -- A) Pick one random prefix per row and all core fields except units and from/to
  base AS (
    SELECT
      c.itemcode,
      c.batchcode,
      (random() < 0.9)::boolean                                           AS batch_enabled,

      now() - random() * interval '180 days'                               AS mfg_date,
      now() + ((30 + (random() * 365))::int) * interval '1 day'            AS exp_date,
      round((1 + random() * 9)::numeric, 2)::real                          AS packed_size,

      -- single random prefix per row
      (ARRAY[
         'sale','consumption','issue','production',
         'salesreturn','purchase','purchasereturn',
         'adjustment','deprecated'
       ])[floor(random() * 9) + 1]                                          AS prefix,

      'qty'::text                                                          AS measurement_unit,
      round((50 + random() * 100)::numeric, 2)::double precision           AS marked_price,
      round((30 + random() * 80)::numeric, 2)::double precision            AS selling_price,
      round((10 + random() * 50)::numeric, 2)::double precision            AS cost_price,

      (random() < 0.3)::boolean                                            AS volume_discounts,
      (1 + floor(random() * 5))::bigint                                    AS suppliercode,
      (random() < 0.3)::boolean                                            AS user_discounts,

      now() - random() * interval '90 days'                                AS last_counted_at,
      substr(md5(random()::text), 1, 10)::text                            AS remarks,
      now() - random() * interval '90 days'                                AS entered_time,

      random()                                                              AS rnd
    FROM
      (VALUES (1,1),(1,2),(2,1),(2,2),(3,1),(3,2)) AS c(itemcode, batchcode)
    CROSS JOIN generate_series(1,100)
  ),

  -- B) Compute units based on that same prefix
  unitized AS (
    SELECT
      b.*,
      CASE
        WHEN b.prefix = 'adjustment' THEN
          CASE WHEN random() < 0.5
               THEN  round((random() * 100)::numeric, 2)::double precision
               ELSE -round((random() * 100)::numeric, 2)::double precision
          END

        WHEN b.prefix IN (
             'sale','consumption','issue',
             'purchasereturn','deprecated'
           )
        THEN -round((random() * 100)::numeric, 2)::double precision

        ELSE
          round((random() * 100)::numeric, 2)::double precision
      END AS units
    FROM base AS b
  ),

  -- C) Attach from_units and compute to_units
  with_from_to AS (
    SELECT
      u.*,
      f.from_units,
      u.units + f.from_units AS to_units
    FROM unitized AS u
    CROSS JOIN LATERAL (
      SELECT round(random() * 100)::double precision AS from_units
    ) AS f
  ),

  -- D) Shuffle and pick the first 500
  numbered AS (
    SELECT
      *,
      row_number() OVER (ORDER BY rnd) AS rn
    FROM with_from_to
  )

-- 3) Insert into the inventory_movements table
INSERT INTO public.inventory_movements (
  itemcode,
  batchcode,
  batch_enabled,
  mfg_date,
  exp_date,
  packed_size,
  units,
  measurement_unit,
  marked_price,
  selling_price,
  cost_price,
  volume_discounts,
  suppliercode,
  user_discounts,
  last_counted_at,
  remarks,
  reference,
  entered_time,
  from_units,
  to_units
)
SELECT
  n.itemcode,
  n.batchcode,
  n.batch_enabled,
  n.mfg_date,
  n.exp_date,
  n.packed_size,
  n.units,
  n.measurement_unit,
  n.marked_price,
  n.selling_price,
  n.cost_price,
  n.volume_discounts,
  n.suppliercode,
  n.user_discounts,
  n.last_counted_at,
  n.remarks,

  -- one-colon reference: same prefix + random 8-char hex
  n.prefix || ':' || substr(md5(random()::text), 1, 8) AS reference,

  n.entered_time,
  n.from_units,
  n.to_units
FROM numbered AS n
WHERE n.rn <= 500;
