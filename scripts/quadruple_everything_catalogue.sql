DELETE FROM public.catalogue
WHERE itemcode>500000;
INSERT INTO catalogue (SELECT 500000+itemcode, 'TOBACCO '||description, active, created_on, 'TOBACCO '||description_pos, 'TOBACCO '||description_web, descriptions_other_languages, default_vat_category, vat_depends_on_user, vat_category_adjustable, price_manual, enforce_above_cost, active_web, expiry_tracking_enabled, permissions_category, categories_bitmask
	FROM public.catalogue);
INSERT INTO catalogue (SELECT 1000000+itemcode, 'ALCOHOL '||description, active, created_on, 'ALCOHOL '||description_pos, 'ALCOHOL '||description_web, descriptions_other_languages, default_vat_category, vat_depends_on_user, vat_category_adjustable, price_manual, enforce_above_cost, active_web, expiry_tracking_enabled, permissions_category, categories_bitmask
	FROM public.catalogue);