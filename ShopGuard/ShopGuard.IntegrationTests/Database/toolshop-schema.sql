-- Schema snapshot of the Toolshop tables that ShopDatabase reads.
-- Taken from the running shop (testsmith/practice-software-testing-sprint5-api) with:
--   mysqldump --no-data toolshop users products invoices invoice_items payments
-- The foreign keys of products to brands, categories and product_images are left out,
-- because those tables are not part of the snapshot. Everything else is unchanged.

CREATE TABLE `users` (
  `id` char(26) NOT NULL,
  `uid` varchar(255) DEFAULT NULL,
  `provider` varchar(255) DEFAULT NULL,
  `first_name` varchar(40) NOT NULL,
  `last_name` varchar(20) NOT NULL,
  `street` varchar(70) DEFAULT NULL,
  `house_number` varchar(10) DEFAULT NULL,
  `city` varchar(40) DEFAULT NULL,
  `state` varchar(40) DEFAULT NULL,
  `country` varchar(40) DEFAULT NULL,
  `postal_code` varchar(10) DEFAULT NULL,
  `phone` varchar(24) DEFAULT NULL,
  `dob` date DEFAULT NULL,
  `email` varchar(256) NOT NULL,
  `password` varchar(255) DEFAULT NULL,
  `role` varchar(255) NOT NULL,
  `enabled` tinyint(1) NOT NULL DEFAULT 1,
  `failed_login_attempts` int(11) NOT NULL DEFAULT 0,
  `totp_secret` varchar(255) DEFAULT NULL,
  `totp_enabled` tinyint(1) NOT NULL DEFAULT 0,
  `totp_verified_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `users_email_unique` (`email`),
  KEY `idx_invoices_users_email` (`email`),
  KEY `idx_invoices_users_failed_login_attempts` (`email`,`failed_login_attempts`),
  KEY `idx_invoices_users_enabled` (`email`,`enabled`),
  FULLTEXT KEY `idx_users_ft` (`first_name`,`last_name`,`email`,`city`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `products` (
  `id` char(26) NOT NULL,
  `name` varchar(220) NOT NULL,
  `description` text DEFAULT NULL,
  `stock` bigint(20) DEFAULT NULL,
  `price` decimal(10,2) NOT NULL,
  `is_location_offer` tinyint(1) NOT NULL,
  `is_rental` tinyint(1) NOT NULL,
  `co2_rating` char(1) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `brand_id` char(26) NOT NULL,
  `category_id` char(26) NOT NULL,
  `product_image_id` char(26) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `products_product_image_id_foreign` (`product_image_id`),
  KEY `products_brand_id_category_id_product_image_id_index` (`brand_id`,`category_id`,`product_image_id`),
  KEY `products_category_id_index` (`category_id`),
  KEY `products_brand_id_index` (`brand_id`),
  KEY `products_is_rental_index` (`is_rental`),
  KEY `products_co2_rating_index` (`co2_rating`),
  KEY `products_name_index` (`name`),
  KEY `products_price_index` (`price`),
  KEY `idx_products_category_brand_rental` (`category_id`,`brand_id`,`is_rental`),
  KEY `idx_products_co2_rating` (`co2_rating`),
  FULLTEXT KEY `idx_products_ft_name` (`name`),
  FULLTEXT KEY `idx_products_ft_name_desc` (`name`,`description`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `invoices` (
  `id` char(26) NOT NULL,
  `invoice_date` datetime NOT NULL,
  `additional_discount_percentage` char(26) DEFAULT NULL,
  `additional_discount_amount` char(26) DEFAULT NULL,
  `eco_discount_percentage` decimal(5,2) DEFAULT NULL,
  `eco_discount_amount` decimal(10,2) DEFAULT NULL,
  `invoice_number` varchar(70) DEFAULT NULL,
  `billing_street` varchar(70) NOT NULL,
  `billing_city` varchar(40) NOT NULL,
  `billing_state` varchar(40) DEFAULT NULL,
  `billing_country` varchar(40) NOT NULL,
  `billing_postal_code` varchar(10) DEFAULT NULL,
  `subtotal` decimal(20,2) DEFAULT NULL,
  `total` decimal(20,2) DEFAULT NULL,
  `status` enum('AWAITING_FULFILLMENT','ON_HOLD','AWAITING_SHIPMENT','SHIPPED','COMPLETED') NOT NULL DEFAULT 'AWAITING_FULFILLMENT',
  `status_message` varchar(255) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `user_id` char(26) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_invoices_user_date` (`user_id`,`invoice_date`),
  KEY `idx_invoices_date` (`invoice_date`),
  CONSTRAINT `invoices_user_id_foreign` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `invoice_items` (
  `id` char(26) NOT NULL,
  `unit_price` decimal(10,2) NOT NULL,
  `quantity` bigint(20) NOT NULL,
  `discount_percentage` decimal(8,2) DEFAULT NULL,
  `discounted_price` decimal(8,2) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `invoice_id` char(26) NOT NULL,
  `product_id` char(26) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_invoice_items_product` (`product_id`),
  KEY `idx_invoice_items_invoice` (`invoice_id`),
  CONSTRAINT `invoice_items_invoice_id_foreign` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`),
  CONSTRAINT `invoice_items_product_id_foreign` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `payments` (
  `id` char(26) NOT NULL,
  `invoice_id` char(26) NOT NULL,
  `payment_method` varchar(40) NOT NULL,
  `payment_details_id` char(26) DEFAULT NULL,
  `payment_details_type` varchar(255) DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `idx_payments_invoice` (`invoice_id`),
  CONSTRAINT `payments_invoice_id_foreign` FOREIGN KEY (`invoice_id`) REFERENCES `invoices` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
