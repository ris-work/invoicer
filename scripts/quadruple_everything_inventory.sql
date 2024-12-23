DELETE FROM public.inventory
WHERE itemcode > 500000;
INSERT INTO inventory (SELECT 500000+itemcode, batchcode, batch_enabled, mfg_date, exp_date, packed_size, units, measurement_unit, marked_price, selling_price, cost_price, volume_discounts, suppliercode, user_discounts
	FROM public.inventory);
INSERT INTO inventory (SELECT 1000000+itemcode, batchcode, batch_enabled, mfg_date, exp_date, packed_size, units, measurement_unit, marked_price, selling_price, cost_price, volume_discounts, suppliercode, user_discounts
	FROM public.inventory);