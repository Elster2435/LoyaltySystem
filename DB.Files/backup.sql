--
-- PostgreSQL database dump
--

\restrict fMauuD4GN1rphib4nRLqkUrpRH7cFwd2l02DFgUP1cQqmEf8JfVL8galhaSNxC3

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: activity_type_enum; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.activity_type_enum AS ENUM (
    'Регистрация',
    'Покупка',
    'Возврат покупки',
    'Начисление бонусов',
    'Списание бонусов',
    'Получено предложение',
    'Использовано предложение',
    'Истекло предложение',
    'Отменено предложение',
    'Применена акция',
    'Изменение профиля'
);


ALTER TYPE public.activity_type_enum OWNER TO postgres;

--
-- Name: bonus_transaction_type_enum; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.bonus_transaction_type_enum AS ENUM (
    'Начисление',
    'Списание',
    'Корректировка',
    'Сгорание'
);


ALTER TYPE public.bonus_transaction_type_enum OWNER TO postgres;

--
-- Name: gender_enum; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.gender_enum AS ENUM (
    'Мужской',
    'Женский'
);


ALTER TYPE public.gender_enum OWNER TO postgres;

--
-- Name: offer_status_enum; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.offer_status_enum AS ENUM (
    'Назначено',
    'Использовано',
    'Истекло',
    'Отменено'
);


ALTER TYPE public.offer_status_enum OWNER TO postgres;

--
-- Name: promotion_type_enum; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.promotion_type_enum AS ENUM (
    'Общая',
    'Персональная',
    'Новый клиент',
    'День рождения',
    'Возврат клиента',
    'Возврат покупки'
);


ALTER TYPE public.promotion_type_enum OWNER TO postgres;

--
-- Name: role_name_enum; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.role_name_enum AS ENUM (
    'Администратор',
    'Менеджер',
    'Аналитик'
);


ALTER TYPE public.role_name_enum OWNER TO postgres;

--
-- Name: status_enum; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.status_enum AS ENUM (
    'Активный',
    'Неактивный',
    'Заблокирован'
);


ALTER TYPE public.status_enum OWNER TO postgres;

--
-- Name: transaction_channel_enum; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.transaction_channel_enum AS ENUM (
    'Оффлайн',
    'Онлайн'
);


ALTER TYPE public.transaction_channel_enum OWNER TO postgres;

--
-- Name: transaction_type_enum; Type: TYPE; Schema: public; Owner: postgres
--

CREATE TYPE public.transaction_type_enum AS ENUM (
    'Покупка',
    'Возврат'
);


ALTER TYPE public.transaction_type_enum OWNER TO postgres;

--
-- Name: fn_assign_birthday_customer_offers(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_assign_birthday_customer_offers() RETURNS integer
    LANGUAGE plpgsql
    AS $$
declare
    v_now timestamp := current_timestamp at time zone 'Asia/Yekaterinburg';
    v_today date := (current_timestamp at time zone 'Asia/Yekaterinburg')::date;
    v_target_date date := ((current_timestamp at time zone 'Asia/Yekaterinburg')::date + 3);

    v_birthday_promotion_id integer;
    v_inserted_count integer := 0;
begin
    select p.promotion_id
    into v_birthday_promotion_id
    from promotions p
    where p.promotion_type = 'День рождения'
      and p.is_active = true
      and p.start_date <= v_today
      and p.end_date >= v_today
    order by p.promotion_id
    limit 1;

    if v_birthday_promotion_id is null then
        return 0;
    end if;

    with target_customers as (
        select c.customer_id
        from customers c
        where c.status = 'Активный'
          and c.birth_date is not null
          and extract(month from c.birth_date) = extract(month from v_target_date)
          and extract(day from c.birth_date) = extract(day from v_target_date)

          and not exists (
              select 1
              from customer_offers co
              join promotions p on p.promotion_id = co.promotion_id
              where co.customer_id = c.customer_id
                and p.promotion_type = 'День рождения'
                and co.offer_status = 'Назначено'
                and co.valid_until >= v_today
          )
    ),
    inserted_offers as (
        insert into customer_offers (
            customer_id,
            promotion_id,
            assigned_at,
            valid_until,
            offer_status
        )
        select
            tc.customer_id,
            v_birthday_promotion_id,
            v_now,
            v_today + 6,
            'Назначено'
        from target_customers tc
        returning offer_id, customer_id
    ),
    inserted_activity as (
        insert into customer_activity (
            customer_id,
            activity_type,
            activity_datetime,
            description
        )
        select
            io.customer_id,
            'Получено предложение',
            v_now,
            'Автоматически назначено персональное предложение ко дню рождения.'
        from inserted_offers io
        returning activity_id
    )
    select count(*)
    into v_inserted_count
    from inserted_offers;

    return v_inserted_count;
end;
$$;


ALTER FUNCTION public.fn_assign_birthday_customer_offers() OWNER TO postgres;

--
-- Name: fn_assign_new_customer_offer(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_assign_new_customer_offer() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
declare
    v_promotion_id int;
begin
    select promotion_id
    into v_promotion_id
    from promotions
    where is_active = true
      and promotion_type = 'Новый клиент'
      and (current_timestamp at time zone 'Asia/Yekaterinburg')::date between start_date and end_date
    order by start_date desc, promotion_id desc
    limit 1;

    if v_promotion_id is null then
        return new;
    end if;

    if exists (
        select 1
        from customer_offers
        where customer_id = new.customer_id
          and promotion_id = v_promotion_id
    ) then
        return new;
    end if;

    insert into customer_offers
    (
        customer_id,
        promotion_id,
        assigned_at,
        valid_until,
        offer_status
    )
    values
    (
        new.customer_id,
        v_promotion_id,
        current_timestamp at time zone 'Asia/Yekaterinburg',
        (current_timestamp at time zone 'Asia/Yekaterinburg') + interval '14 days',
        'Назначено'
    );

    insert into customer_activity
    (
        customer_id,
        activity_type,
        activity_datetime,
        description
    )
    values
    (
        new.customer_id,
        'Получено предложение',
        current_timestamp at time zone 'Asia/Yekaterinburg',
        'Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней'
    );

    return new;
end;
$$;


ALTER FUNCTION public.fn_assign_new_customer_offer() OWNER TO postgres;

--
-- Name: fn_create_loyalty_account_after_customer(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_create_loyalty_account_after_customer() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
declare
    v_base_level_id int;
begin
    select level_id
    into v_base_level_id
    from loyalty_levels
    order by min_total_spent
    limit 1;

    if v_base_level_id is null then
        raise exception 'Не найден базовый уровень лояльности';
    end if;

    insert into customer_loyalty_accounts
    (
        customer_id,
        level_id,
        bonus_balance,
        total_spent,
        created_at,
        account_status
    )
    values
    (
        new.customer_id,
        v_base_level_id,
        0,
        0,
        current_timestamp at time zone 'Asia/Yekaterinburg',
        'Активный'
    );

    insert into customer_activity
    (
        customer_id,
        activity_type,
        activity_datetime,
        description
    )
    values
    (
        new.customer_id,
        'Регистрация',
        current_timestamp at time zone 'Asia/Yekaterinburg',
        'Клиент зарегистрирован в программе лояльности'
    );

    return new;
end;
$$;


ALTER FUNCTION public.fn_create_loyalty_account_after_customer() OWNER TO postgres;

--
-- Name: fn_expire_overdue_customer_offers(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_expire_overdue_customer_offers() RETURNS integer
    LANGUAGE plpgsql
    AS $$
declare
    v_expired_count integer := 0;
begin
    insert into customer_activity
    (
        customer_id,
        activity_type,
        activity_datetime,
        description
    )
    select
        co.customer_id,
        'Истекло предложение',
        current_timestamp at time zone 'Asia/Yekaterinburg',
        'Срок действия персонального предложения "' || p.promotion_name || '" истек'
    from customer_offers co
    join promotions p
        on co.promotion_id = p.promotion_id
    where co.offer_status = 'Назначено'
      and co.valid_until is not null
      and co.valid_until::date < (current_timestamp at time zone 'Asia/Yekaterinburg')::date;

    update customer_offers co
    set offer_status = 'Истекло'
    where co.offer_status = 'Назначено'
      and co.valid_until is not null
      and co.valid_until::date < (current_timestamp at time zone 'Asia/Yekaterinburg')::date;

    get diagnostics v_expired_count = row_count;

    return v_expired_count;
end;
$$;


ALTER FUNCTION public.fn_expire_overdue_customer_offers() OWNER TO postgres;

--
-- Name: fn_prepare_transaction_before_insert(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_prepare_transaction_before_insert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
declare
    v_account_id int;
    v_level_id int;
    v_level_min_total_spent numeric(12,2);
    v_bonus_percent numeric(5,2);
    v_bonus_balance numeric(12,2);

    v_paid_amount numeric(12,2);
    v_base_bonus numeric(12,2);

    v_bonus_multiplier numeric(5,2) := 1;
    v_extra_bonus numeric(12,2) := 0;

    v_required_level_id int;
    v_required_level_min_total_spent numeric(12,2);

    v_promotion_name varchar(200);
    v_promotion_start_date date;
    v_promotion_end_date date;
    v_promotion_is_active boolean;

    v_transaction_date date;

    v_original transactions%rowtype;

    v_available_bonus numeric(12,2);
    v_missing_bonus numeric(12,2);
    v_return_paid_amount numeric(12,2);
begin
    if new.transaction_type is null then
        new.transaction_type := 'Покупка';
    end if;

    if new.transaction_amount < 0 then
        raise exception 'Сумма транзакции не может быть отрицательной';
    end if;

    if new.bonus_used < 0 then
        raise exception 'Количество списанных бонусов не может быть отрицательным';
    end if;

    if new.bonus_accrued < 0 then
        raise exception 'Количество начисленных бонусов не может быть отрицательным';
    end if;

    if new.paid_amount < 0 then
        raise exception 'Оплаченная сумма не может быть отрицательной';
    end if;

    if new.bonus_compensation_amount < 0 then
        raise exception 'Сумма удержания при возврате не может быть отрицательной';
    end if;

    if new.transaction_datetime is null then
        new.transaction_datetime := current_timestamp at time zone 'Asia/Yekaterinburg';
    end if;

    v_transaction_date := new.transaction_datetime::date;


    -- --------------------------------------------------------
    -- ПОКУПКА
    -- --------------------------------------------------------
    if new.transaction_type = 'Покупка' then

        if new.original_transaction_id is not null then
            raise exception 'Покупка не должна ссылаться на исходную транзакцию';
        end if;

        if new.customer_id is null then
            raise exception 'Для покупки должен быть указан клиент';
        end if;

        if new.promotion_id is not null and new.offer_id is not null then
            raise exception 'Нельзя одновременно применить акцию и персональное предложение';
        end if;

        select
            cla.account_id,
            cla.level_id,
            cla.bonus_balance,
            ll.min_total_spent,
            ll.bonus_percent
        into
            v_account_id,
            v_level_id,
            v_bonus_balance,
            v_level_min_total_spent,
            v_bonus_percent
        from customer_loyalty_accounts cla
        join loyalty_levels ll
            on cla.level_id = ll.level_id
        where cla.customer_id = new.customer_id
          and cla.account_status = 'Активный'
        for update of cla;

        if v_account_id is null then
            raise exception 'Для клиента % не найден активный бонусный счет', new.customer_id;
        end if;

        if new.bonus_used > v_bonus_balance then
            raise exception
                'Недостаточно бонусов для списания. Баланс: %, попытка списания: %',
                v_bonus_balance,
                new.bonus_used;
        end if;

        if new.bonus_used > round(new.transaction_amount * 0.2, 2) then
            raise exception
                'Нельзя списать более 20%% от суммы покупки. Максимум: %, попытка списания: %',
                round(new.transaction_amount * 0.2, 2),
                new.bonus_used;
        end if;

        v_paid_amount := new.transaction_amount - new.bonus_used;

        if v_paid_amount < 0 then
            raise exception 'Списанные бонусы не могут превышать сумму покупки';
        end if;

        new.paid_amount := v_paid_amount;
        new.bonus_compensation_amount := 0;


        -- Персональное предложение
        if new.offer_id is not null then

            select
                p.promotion_name,
                p.bonus_multiplier,
                p.extra_bonus,
                p.required_level_id,
                p.start_date,
                p.end_date,
                p.is_active
            into
                v_promotion_name,
                v_bonus_multiplier,
                v_extra_bonus,
                v_required_level_id,
                v_promotion_start_date,
                v_promotion_end_date,
                v_promotion_is_active
            from customer_offers co
            join promotions p
                on co.promotion_id = p.promotion_id
            where co.offer_id = new.offer_id
              and co.customer_id = new.customer_id
              and co.offer_status = 'Назначено'
              and (
                    co.valid_until is null
                    or co.valid_until::date >= v_transaction_date
                  );

            if v_promotion_name is null then
                raise exception
                    'Персональное предложение % недоступно для клиента %',
                    new.offer_id,
                    new.customer_id;
            end if;


        -- Общая акция
        elsif new.promotion_id is not null then

            select
                p.promotion_name,
                p.bonus_multiplier,
                p.extra_bonus,
                p.required_level_id,
                p.start_date,
                p.end_date,
                p.is_active
            into
                v_promotion_name,
                v_bonus_multiplier,
                v_extra_bonus,
                v_required_level_id,
                v_promotion_start_date,
                v_promotion_end_date,
                v_promotion_is_active
            from promotions p
            where p.promotion_id = new.promotion_id;

            if v_promotion_name is null then
                raise exception 'Акция % не найдена', new.promotion_id;
            end if;
        end if;


        -- Проверки акции / предложения
        if new.promotion_id is not null or new.offer_id is not null then

            if v_promotion_is_active = false then
                raise exception 'Выбранная акция отключена';
            end if;

            if v_transaction_date < v_promotion_start_date
               or v_transaction_date > v_promotion_end_date then
                raise exception 'Выбранная акция недействительна на дату покупки';
            end if;

            if v_required_level_id is not null then
                select min_total_spent
                into v_required_level_min_total_spent
                from loyalty_levels
                where level_id = v_required_level_id;

                if v_required_level_min_total_spent is null then
                    raise exception 'Требуемый уровень акции не найден';
                end if;

                if v_level_min_total_spent < v_required_level_min_total_spent then
                    raise exception 'Уровень клиента недостаточен для применения выбранной акции';
                end if;
            end if;
        end if;

        v_base_bonus := new.paid_amount * v_bonus_percent / 100;

        new.bonus_accrued := round(v_base_bonus * v_bonus_multiplier + v_extra_bonus, 2);

        return new;
    end if;


    -- --------------------------------------------------------
    -- ВОЗВРАТ
    -- --------------------------------------------------------
    if new.transaction_type = 'Возврат' then

        if new.original_transaction_id is null then
            raise exception 'Для возврата должна быть указана исходная покупка';
        end if;

        select *
        into v_original
        from transactions
        where transaction_id = new.original_transaction_id;

        if v_original.transaction_id is null then
            raise exception 'Исходная транзакция % не найдена', new.original_transaction_id;
        end if;

        if v_original.transaction_type <> 'Покупка' then
            raise exception 'Возврат можно оформить только по транзакции типа "Покупка"';
        end if;

        if v_original.customer_id is null then
            raise exception 'Нельзя оформить возврат: у исходной покупки не указан клиент';
        end if;

        if exists (
            select 1
            from transactions
            where transaction_type = 'Возврат'
              and original_transaction_id = new.original_transaction_id
        ) then
            raise exception 'По покупке % уже оформлен возврат', new.original_transaction_id;
        end if;

        if new.customer_id is null then
            new.customer_id := v_original.customer_id;
        end if;

        if new.customer_id <> v_original.customer_id then
            raise exception 'Клиент возврата должен совпадать с клиентом исходной покупки';
        end if;

        select
            cla.account_id,
            cla.bonus_balance
        into
            v_account_id,
            v_bonus_balance
        from customer_loyalty_accounts cla
        where cla.customer_id = new.customer_id
          and cla.account_status = 'Активный'
        for update of cla;

        if v_account_id is null then
            raise exception 'Для клиента % не найден активный бонусный счет', new.customer_id;
        end if;

        /*
            Новая логика возврата:
            - сначала считаем, сколько бонусов доступно для аннулирования;
            - доступно = текущий баланс + бонусы, которые были списаны в исходной покупке и будут возвращены;
            - если доступных бонусов не хватает, недостающая часть удерживается из денежного возврата.
        */
        v_available_bonus := v_bonus_balance + v_original.bonus_used;

        if v_available_bonus >= v_original.bonus_accrued then
            v_missing_bonus := 0;
        else
            v_missing_bonus := v_original.bonus_accrued - v_available_bonus;
        end if;

        v_return_paid_amount := v_original.paid_amount - v_missing_bonus;

        if v_return_paid_amount < 0 then
            raise exception
                'Невозможно оформить возврат: сумма недостающих бонусов (%) больше суммы денежного возврата (%)',
                v_missing_bonus,
                v_original.paid_amount;
        end if;

        new.transaction_amount := v_original.transaction_amount;
        new.bonus_used := v_original.bonus_used;
        new.paid_amount := v_return_paid_amount;
        new.bonus_accrued := v_original.bonus_accrued;
        new.bonus_compensation_amount := v_missing_bonus;

        /*
            Возврат не должен повторно применять акцию или предложение.
            Он только ссылается на исходную покупку.
        */
        new.promotion_id := null;
        new.offer_id := null;

        return new;
    end if;

    raise exception 'Неизвестный тип транзакции: %', new.transaction_type;
end;
$$;


ALTER FUNCTION public.fn_prepare_transaction_before_insert() OWNER TO postgres;

--
-- Name: fn_process_inactive_customers(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_process_inactive_customers() RETURNS TABLE(return_offers_created integer, customers_deactivated integer)
    LANGUAGE plpgsql
    AS $$
declare
    v_now timestamp := current_timestamp at time zone 'Asia/Yekaterinburg';
    v_today date := (current_timestamp at time zone 'Asia/Yekaterinburg')::date;

    v_return_promotion_id integer;
    v_created_offers integer := 0;
    v_deactivated_customers integer := 0;
begin
    select p.promotion_id
    into v_return_promotion_id
    from promotions p
    where p.promotion_type = 'Возврат клиента'
      and p.is_active = true
      and p.start_date <= v_today
      and p.end_date >= v_today
    order by p.promotion_id
    limit 1;

    if v_return_promotion_id is not null then
        with customer_last_purchase as (
            select
                c.customer_id,
                coalesce(
                    max(t.transaction_datetime)::date,
                    c.registration_date::date
                ) as last_purchase_or_registration_date
            from customers c
            left join transactions t
                on t.customer_id = c.customer_id
               and t.transaction_type = 'Покупка'
            where c.status = 'Активный'
            group by c.customer_id, c.registration_date
        ),
        target_customers as (
            select clp.customer_id
            from customer_last_purchase clp
            where (v_today - clp.last_purchase_or_registration_date) >= 30
              and (v_today - clp.last_purchase_or_registration_date) < 60
              and not exists (
                  select 1
                  from customer_offers co
                  join promotions p on p.promotion_id = co.promotion_id
                  where co.customer_id = clp.customer_id
                    and p.promotion_type = 'Возврат клиента'
                    and co.offer_status = 'Назначено'
                    and co.valid_until >= v_today
              )
        ),
        inserted_offers as (
            insert into customer_offers (
                customer_id,
                promotion_id,
                assigned_at,
                valid_until,
                offer_status
            )
            select
                tc.customer_id,
                v_return_promotion_id,
                v_now,
                v_today + 30,
                'Назначено'
            from target_customers tc
            returning offer_id, customer_id
        ),
        inserted_activity as (
            insert into customer_activity (
                customer_id,
                activity_type,
                activity_datetime,
                description
            )
            select
                io.customer_id,
                'Получено предложение',
                v_now,
                'Автоматически назначено персональное предложение для возврата клиента.'
            from inserted_offers io
            returning activity_id
        )
        select count(*)
        into v_created_offers
        from inserted_offers;
    end if;

    with customer_last_purchase as (
        select
            c.customer_id,
            coalesce(
                max(t.transaction_datetime)::date,
                c.registration_date::date
            ) as last_purchase_or_registration_date
        from customers c
        left join transactions t
            on t.customer_id = c.customer_id
           and t.transaction_type = 'Покупка'
        where c.status = 'Активный'
        group by c.customer_id, c.registration_date
    ),
    target_customers as (
        select clp.customer_id
        from customer_last_purchase clp
        where (v_today - clp.last_purchase_or_registration_date) >= 60
    ),
    deactivated_customers as (
        update customers c
        set status = 'Неактивный',
            updated_at = v_now
        from target_customers tc
        where c.customer_id = tc.customer_id
        returning c.customer_id
    ),
    updated_accounts as (
        update customer_loyalty_accounts cla
        set account_status = 'Неактивный'
        from deactivated_customers dc
        where cla.customer_id = dc.customer_id
        returning cla.account_id, cla.customer_id
    ),
    inserted_activity as (
        insert into customer_activity (
            customer_id,
            activity_type,
            activity_datetime,
            description
        )
        select
            dc.customer_id,
            'Изменение профиля',
            v_now,
            'Клиент автоматически переведен в статус "Неактивный" из-за отсутствия покупок 60 дней.'
        from deactivated_customers dc
        returning activity_id
    )
    select count(*)
    into v_deactivated_customers
    from deactivated_customers;

    return query
    select v_created_offers, v_deactivated_customers;
end;
$$;


ALTER FUNCTION public.fn_process_inactive_customers() OWNER TO postgres;

--
-- Name: fn_process_transaction_after_insert(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_process_transaction_after_insert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
declare
    v_account_id int;
    v_total_spent numeric(12,2);
    v_new_level_id int;

    v_promotion_name varchar(200);
    v_offer_promotion_name varchar(200);

    v_compensation_promotion_id int;

    v_bonus_to_cancel_from_balance numeric(12,2);
begin
    select account_id
    into v_account_id
    from customer_loyalty_accounts
    where customer_id = new.customer_id
    for update;

    if v_account_id is null then
        raise exception 'Бонусный счет клиента % не найден', new.customer_id;
    end if;


    -- --------------------------------------------------------
    -- ПОКУПКА
    -- --------------------------------------------------------
    if new.transaction_type = 'Покупка' then

        update customer_loyalty_accounts
        set
            bonus_balance = bonus_balance - new.bonus_used + new.bonus_accrued,
            total_spent = total_spent + new.paid_amount
        where account_id = v_account_id
        returning total_spent into v_total_spent;

        select level_id
        into v_new_level_id
        from loyalty_levels
        where min_total_spent <= v_total_spent
        order by min_total_spent desc
        limit 1;

        if v_new_level_id is not null then
            update customer_loyalty_accounts
            set level_id = v_new_level_id
            where account_id = v_account_id;
        end if;

        insert into customer_activity
        (
            customer_id,
            activity_type,
            activity_datetime,
            description
        )
        values
        (
            new.customer_id,
            'Покупка',
            current_timestamp at time zone 'Asia/Yekaterinburg',
            'Оформлена покупка №' || new.transaction_id ||
            ' на сумму ' || new.transaction_amount ||
            '. Оплачено деньгами: ' || new.paid_amount ||
            '. Списано бонусов: ' || new.bonus_used
        );

        if new.bonus_used > 0 then
            insert into bonus_transactions
            (
                account_id,
                transaction_id,
                bonus_transaction_type,
                amount,
                bonus_transaction_datetime,
                description
            )
            values
            (
                v_account_id,
                new.transaction_id,
                'Списание',
                new.bonus_used,
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'Списание бонусов при покупке №' || new.transaction_id
            );

            insert into customer_activity
            (
                customer_id,
                activity_type,
                activity_datetime,
                description
            )
            values
            (
                new.customer_id,
                'Списание бонусов',
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'При покупке №' || new.transaction_id || ' списано бонусов: ' || new.bonus_used
            );
        end if;

        if new.bonus_accrued > 0 then
            insert into bonus_transactions
            (
                account_id,
                transaction_id,
                bonus_transaction_type,
                amount,
                bonus_transaction_datetime,
                description
            )
            values
            (
                v_account_id,
                new.transaction_id,
                'Начисление',
                new.bonus_accrued,
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'Начисление бонусов за покупку №' || new.transaction_id
            );

            insert into customer_activity
            (
                customer_id,
                activity_type,
                activity_datetime,
                description
            )
            values
            (
                new.customer_id,
                'Начисление бонусов',
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'За покупку №' || new.transaction_id || ' начислено бонусов: ' || new.bonus_accrued
            );
        end if;

        -- Если использовано персональное предложение
        if new.offer_id is not null then

            select p.promotion_name
            into v_offer_promotion_name
            from customer_offers co
            join promotions p
                on co.promotion_id = p.promotion_id
            where co.offer_id = new.offer_id;

            update customer_offers
            set offer_status = 'Использовано'
            where offer_id = new.offer_id
              and offer_status = 'Назначено';

            insert into customer_activity
            (
                customer_id,
                activity_type,
                activity_datetime,
                description
            )
            values
            (
                new.customer_id,
                'Использовано предложение',
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'При покупке №' || new.transaction_id ||
                ' использовано персональное предложение "' ||
                coalesce(v_offer_promotion_name, 'Без названия') || '"'
            );
        end if;

        -- Если применена общая акция
        if new.promotion_id is not null then

            select promotion_name
            into v_promotion_name
            from promotions
            where promotion_id = new.promotion_id;

            insert into customer_activity
            (
                customer_id,
                activity_type,
                activity_datetime,
                description
            )
            values
            (
                new.customer_id,
                'Применена акция',
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'При покупке №' || new.transaction_id ||
                ' применена акция "' ||
                coalesce(v_promotion_name, 'Без названия') || '"'
            );
        end if;

        return new;
    end if;


    -- --------------------------------------------------------
    -- ВОЗВРАТ
    -- --------------------------------------------------------
    if new.transaction_type = 'Возврат' then

        /*
            new.bonus_accrued — сколько всего бонусов нужно аннулировать по исходной покупке.
            new.bonus_compensation_amount — часть, которую не смогли списать бонусами и удержали деньгами.
            v_bonus_to_cancel_from_balance — сколько реально списываем с бонусного счета.
        */
        v_bonus_to_cancel_from_balance := new.bonus_accrued - new.bonus_compensation_amount;

        if v_bonus_to_cancel_from_balance < 0 then
            v_bonus_to_cancel_from_balance := 0;
        end if;

        update customer_loyalty_accounts
        set
            bonus_balance = bonus_balance + new.bonus_used - v_bonus_to_cancel_from_balance,
            total_spent = greatest(total_spent - new.paid_amount, 0)
        where account_id = v_account_id
        returning total_spent into v_total_spent;

        select level_id
        into v_new_level_id
        from loyalty_levels
        where min_total_spent <= v_total_spent
        order by min_total_spent desc
        limit 1;

        if v_new_level_id is not null then
            update customer_loyalty_accounts
            set level_id = v_new_level_id
            where account_id = v_account_id;
        end if;

        insert into customer_activity
        (
            customer_id,
            activity_type,
            activity_datetime,
            description
        )
        values
        (
            new.customer_id,
            'Возврат покупки',
            current_timestamp at time zone 'Asia/Yekaterinburg',
            'Оформлен возврат по покупке №' ||
            new.original_transaction_id ||
            '. Сумма исходной покупки: ' ||
            new.transaction_amount ||
            '. Возвращено деньгами: ' ||
            new.paid_amount ||
            '. Удержано за недостающие бонусы: ' ||
            new.bonus_compensation_amount
        );

        if new.bonus_used > 0 then
            insert into bonus_transactions
            (
                account_id,
                transaction_id,
                bonus_transaction_type,
                amount,
                bonus_transaction_datetime,
                description
            )
            values
            (
                v_account_id,
                new.transaction_id,
                'Корректировка',
                new.bonus_used,
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'Возврат бонусов, списанных при покупке №' || new.original_transaction_id
            );

            insert into customer_activity
            (
                customer_id,
                activity_type,
                activity_datetime,
                description
            )
            values
            (
                new.customer_id,
                'Начисление бонусов',
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'При возврате покупки №' ||
                new.original_transaction_id ||
                ' клиенту возвращено бонусов: ' ||
                new.bonus_used
            );
        end if;

        if v_bonus_to_cancel_from_balance > 0 then
            insert into bonus_transactions
            (
                account_id,
                transaction_id,
                bonus_transaction_type,
                amount,
                bonus_transaction_datetime,
                description
            )
            values
            (
                v_account_id,
                new.transaction_id,
                'Корректировка',
                v_bonus_to_cancel_from_balance,
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'Аннулирование бонусов, начисленных за покупку №' || new.original_transaction_id
            );

            insert into customer_activity
            (
                customer_id,
                activity_type,
                activity_datetime,
                description
            )
            values
            (
                new.customer_id,
                'Списание бонусов',
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'При возврате покупки №' ||
                new.original_transaction_id ||
                ' аннулировано бонусов с бонусного счета: ' ||
                v_bonus_to_cancel_from_balance
            );
        end if;

        if new.bonus_compensation_amount > 0 then
            insert into customer_activity
            (
                customer_id,
                activity_type,
                activity_datetime,
                description
            )
            values
            (
                new.customer_id,
                'Возврат покупки',
                current_timestamp at time zone 'Asia/Yekaterinburg',
                'При возврате покупки №' ||
                new.original_transaction_id ||
                ' удержано ' ||
                new.bonus_compensation_amount ||
                ' руб. из денежного возврата из-за нехватки бонусов для аннулирования'
            );
        end if;


        -- Назначение компенсационного предложения после возврата
        select promotion_id
        into v_compensation_promotion_id
        from promotions
        where is_active = true
          and promotion_type = 'Возврат покупки'
          and (current_timestamp at time zone 'Asia/Yekaterinburg')::date between start_date and end_date
        order by start_date desc, promotion_id desc
        limit 1;

        if v_compensation_promotion_id is not null then

            if not exists (
                select 1
                from customer_offers
                where customer_id = new.customer_id
                  and promotion_id = v_compensation_promotion_id
                  and offer_status = 'Назначено'
                  and (
                        valid_until is null
                        or valid_until::date >= (current_timestamp at time zone 'Asia/Yekaterinburg')::date
                      )
            ) then

                insert into customer_offers
                (
                    customer_id,
                    promotion_id,
                    assigned_at,
                    valid_until,
                    offer_status
                )
                values
                (
                    new.customer_id,
                    v_compensation_promotion_id,
                    current_timestamp at time zone 'Asia/Yekaterinburg',
                    (current_timestamp at time zone 'Asia/Yekaterinburg') + interval '14 days',
                    'Назначено'
                );

                insert into customer_activity
                (
                    customer_id,
                    activity_type,
                    activity_datetime,
                    description
                )
                values
                (
                    new.customer_id,
                    'Получено предложение',
                    current_timestamp at time zone 'Asia/Yekaterinburg',
                    'После возврата покупки клиенту назначено компенсационное персональное предложение на 14 дней'
                );
            end if;
        end if;

        return new;
    end if;

    return new;
end;
$$;


ALTER FUNCTION public.fn_process_transaction_after_insert() OWNER TO postgres;

--
-- Name: fn_set_updated_at(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_set_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
begin
    new.updated_at := current_timestamp at time zone 'Asia/Yekaterinburg';
    return new;
end;
$$;


ALTER FUNCTION public.fn_set_updated_at() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: bonus_transactions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.bonus_transactions (
    bonus_transaction_id integer NOT NULL,
    account_id integer NOT NULL,
    transaction_id integer,
    bonus_transaction_type public.bonus_transaction_type_enum NOT NULL,
    amount numeric(12,2) NOT NULL,
    bonus_transaction_datetime timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Yekaterinburg'::text) NOT NULL,
    description text,
    CONSTRAINT chk_bonus_amount CHECK ((amount >= (0)::numeric))
);


ALTER TABLE public.bonus_transactions OWNER TO postgres;

--
-- Name: bonus_transactions_bonus_transaction_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.bonus_transactions_bonus_transaction_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.bonus_transactions_bonus_transaction_id_seq OWNER TO postgres;

--
-- Name: bonus_transactions_bonus_transaction_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.bonus_transactions_bonus_transaction_id_seq OWNED BY public.bonus_transactions.bonus_transaction_id;


--
-- Name: customer_activity; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.customer_activity (
    activity_id integer NOT NULL,
    customer_id integer,
    activity_type public.activity_type_enum NOT NULL,
    activity_datetime timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Yekaterinburg'::text) NOT NULL,
    description text
);


ALTER TABLE public.customer_activity OWNER TO postgres;

--
-- Name: customer_activity_activity_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.customer_activity_activity_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.customer_activity_activity_id_seq OWNER TO postgres;

--
-- Name: customer_activity_activity_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.customer_activity_activity_id_seq OWNED BY public.customer_activity.activity_id;


--
-- Name: customer_loyalty_accounts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.customer_loyalty_accounts (
    account_id integer NOT NULL,
    customer_id integer NOT NULL,
    level_id integer NOT NULL,
    bonus_balance numeric(12,2) DEFAULT 0 NOT NULL,
    total_spent numeric(12,2) DEFAULT 0 NOT NULL,
    created_at timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Yekaterinburg'::text) NOT NULL,
    account_status public.status_enum NOT NULL,
    CONSTRAINT chk_bonus_balance CHECK ((bonus_balance >= (0)::numeric)),
    CONSTRAINT chk_total_spent CHECK ((total_spent >= (0)::numeric))
);


ALTER TABLE public.customer_loyalty_accounts OWNER TO postgres;

--
-- Name: customer_loyalty_accounts_account_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.customer_loyalty_accounts_account_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.customer_loyalty_accounts_account_id_seq OWNER TO postgres;

--
-- Name: customer_loyalty_accounts_account_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.customer_loyalty_accounts_account_id_seq OWNED BY public.customer_loyalty_accounts.account_id;


--
-- Name: customer_offers; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.customer_offers (
    offer_id integer NOT NULL,
    customer_id integer NOT NULL,
    promotion_id integer NOT NULL,
    assigned_at timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Yekaterinburg'::text) NOT NULL,
    valid_until timestamp without time zone,
    offer_status public.offer_status_enum DEFAULT 'Назначено'::public.offer_status_enum NOT NULL
);


ALTER TABLE public.customer_offers OWNER TO postgres;

--
-- Name: customer_offers_offer_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.customer_offers_offer_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.customer_offers_offer_id_seq OWNER TO postgres;

--
-- Name: customer_offers_offer_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.customer_offers_offer_id_seq OWNED BY public.customer_offers.offer_id;


--
-- Name: customers; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.customers (
    customer_id integer NOT NULL,
    last_name character varying(64) NOT NULL,
    first_name character varying(64) NOT NULL,
    middle_name character varying(64),
    phone character varying(20) NOT NULL,
    email character varying(128) NOT NULL,
    birth_date date,
    gender public.gender_enum NOT NULL,
    registration_date timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Yekaterinburg'::text) NOT NULL,
    status public.status_enum NOT NULL,
    updated_at timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Yekaterinburg'::text) NOT NULL
);


ALTER TABLE public.customers OWNER TO postgres;

--
-- Name: customers_customer_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.customers_customer_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.customers_customer_id_seq OWNER TO postgres;

--
-- Name: customers_customer_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.customers_customer_id_seq OWNED BY public.customers.customer_id;


--
-- Name: loyalty_levels; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.loyalty_levels (
    level_id integer NOT NULL,
    level_name character varying(50) NOT NULL,
    min_total_spent numeric(12,2) DEFAULT 0 NOT NULL,
    bonus_percent numeric(5,2) DEFAULT 0 NOT NULL,
    CONSTRAINT chk_bonus_percent CHECK (((bonus_percent >= (0)::numeric) AND (bonus_percent <= (100)::numeric))),
    CONSTRAINT chk_min_total_spent CHECK ((min_total_spent >= (0)::numeric))
);


ALTER TABLE public.loyalty_levels OWNER TO postgres;

--
-- Name: loyalty_levels_level_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.loyalty_levels_level_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.loyalty_levels_level_id_seq OWNER TO postgres;

--
-- Name: loyalty_levels_level_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.loyalty_levels_level_id_seq OWNED BY public.loyalty_levels.level_id;


--
-- Name: promotions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.promotions (
    promotion_id integer NOT NULL,
    promotion_type public.promotion_type_enum NOT NULL,
    promotion_name character varying(200) NOT NULL,
    description text,
    start_date date NOT NULL,
    end_date date NOT NULL,
    bonus_multiplier numeric(5,2) DEFAULT 1 NOT NULL,
    extra_bonus numeric(12,2) DEFAULT 0 NOT NULL,
    required_level_id integer,
    is_active boolean DEFAULT true NOT NULL,
    CONSTRAINT chk_bonus_multiplier CHECK ((bonus_multiplier >= (1)::numeric)),
    CONSTRAINT chk_extra_bonus CHECK ((extra_bonus >= (0)::numeric)),
    CONSTRAINT chk_promotion_dates CHECK ((end_date >= start_date))
);


ALTER TABLE public.promotions OWNER TO postgres;

--
-- Name: promotions_promotion_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.promotions_promotion_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.promotions_promotion_id_seq OWNER TO postgres;

--
-- Name: promotions_promotion_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.promotions_promotion_id_seq OWNED BY public.promotions.promotion_id;


--
-- Name: roles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.roles (
    role_id integer NOT NULL,
    role_name public.role_name_enum NOT NULL
);


ALTER TABLE public.roles OWNER TO postgres;

--
-- Name: roles_role_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.roles_role_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.roles_role_id_seq OWNER TO postgres;

--
-- Name: roles_role_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.roles_role_id_seq OWNED BY public.roles.role_id;


--
-- Name: transactions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.transactions (
    transaction_id integer NOT NULL,
    transaction_type public.transaction_type_enum DEFAULT 'Покупка'::public.transaction_type_enum NOT NULL,
    original_transaction_id integer,
    customer_id integer,
    transaction_datetime timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Yekaterinburg'::text) NOT NULL,
    transaction_amount numeric(12,2) NOT NULL,
    bonus_used numeric(12,2) DEFAULT 0 NOT NULL,
    paid_amount numeric(12,2) DEFAULT 0 NOT NULL,
    bonus_accrued numeric(12,2) DEFAULT 0 NOT NULL,
    transaction_channel public.transaction_channel_enum DEFAULT 'Оффлайн'::public.transaction_channel_enum NOT NULL,
    promotion_id integer,
    offer_id integer,
    comment text,
    bonus_compensation_amount numeric(12,2) DEFAULT 0 NOT NULL,
    CONSTRAINT chk_bonus_accrued CHECK ((bonus_accrued >= (0)::numeric)),
    CONSTRAINT chk_bonus_compensation_amount CHECK ((bonus_compensation_amount >= (0)::numeric)),
    CONSTRAINT chk_bonus_used CHECK ((bonus_used >= (0)::numeric)),
    CONSTRAINT chk_paid_amount CHECK ((paid_amount >= (0)::numeric)),
    CONSTRAINT chk_transaction_amount CHECK ((transaction_amount >= (0)::numeric)),
    CONSTRAINT chk_transaction_not_self_return CHECK (((original_transaction_id IS NULL) OR (original_transaction_id <> transaction_id))),
    CONSTRAINT chk_transaction_original CHECK ((((transaction_type = 'Покупка'::public.transaction_type_enum) AND (original_transaction_id IS NULL)) OR ((transaction_type = 'Возврат'::public.transaction_type_enum) AND (original_transaction_id IS NOT NULL))))
);


ALTER TABLE public.transactions OWNER TO postgres;

--
-- Name: transactions_transaction_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.transactions_transaction_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.transactions_transaction_id_seq OWNER TO postgres;

--
-- Name: transactions_transaction_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.transactions_transaction_id_seq OWNED BY public.transactions.transaction_id;


--
-- Name: users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.users (
    user_id integer NOT NULL,
    last_name character varying(64) NOT NULL,
    first_name character varying(64) NOT NULL,
    middle_name character varying(64),
    login character varying(32) NOT NULL,
    password_hash text NOT NULL,
    role_id integer NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Yekaterinburg'::text) NOT NULL,
    updated_at timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Yekaterinburg'::text) NOT NULL
);


ALTER TABLE public.users OWNER TO postgres;

--
-- Name: users_user_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.users_user_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.users_user_id_seq OWNER TO postgres;

--
-- Name: users_user_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.users_user_id_seq OWNED BY public.users.user_id;


--
-- Name: bonus_transactions bonus_transaction_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bonus_transactions ALTER COLUMN bonus_transaction_id SET DEFAULT nextval('public.bonus_transactions_bonus_transaction_id_seq'::regclass);


--
-- Name: customer_activity activity_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_activity ALTER COLUMN activity_id SET DEFAULT nextval('public.customer_activity_activity_id_seq'::regclass);


--
-- Name: customer_loyalty_accounts account_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_loyalty_accounts ALTER COLUMN account_id SET DEFAULT nextval('public.customer_loyalty_accounts_account_id_seq'::regclass);


--
-- Name: customer_offers offer_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_offers ALTER COLUMN offer_id SET DEFAULT nextval('public.customer_offers_offer_id_seq'::regclass);


--
-- Name: customers customer_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customers ALTER COLUMN customer_id SET DEFAULT nextval('public.customers_customer_id_seq'::regclass);


--
-- Name: loyalty_levels level_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.loyalty_levels ALTER COLUMN level_id SET DEFAULT nextval('public.loyalty_levels_level_id_seq'::regclass);


--
-- Name: promotions promotion_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.promotions ALTER COLUMN promotion_id SET DEFAULT nextval('public.promotions_promotion_id_seq'::regclass);


--
-- Name: roles role_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.roles ALTER COLUMN role_id SET DEFAULT nextval('public.roles_role_id_seq'::regclass);


--
-- Name: transactions transaction_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.transactions ALTER COLUMN transaction_id SET DEFAULT nextval('public.transactions_transaction_id_seq'::regclass);


--
-- Name: users user_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users ALTER COLUMN user_id SET DEFAULT nextval('public.users_user_id_seq'::regclass);


--
-- Data for Name: bonus_transactions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.bonus_transactions (bonus_transaction_id, account_id, transaction_id, bonus_transaction_type, amount, bonus_transaction_datetime, description) FROM stdin;
\.


--
-- Data for Name: customer_activity; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.customer_activity (activity_id, customer_id, activity_type, activity_datetime, description) FROM stdin;
1	1	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
2	1	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
3	2	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
4	2	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
5	3	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
6	3	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
7	4	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
8	4	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
9	5	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
10	5	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
11	6	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
12	6	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
13	7	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
14	7	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
15	8	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
16	8	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
17	9	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
18	9	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
19	10	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
20	10	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
21	11	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
22	11	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
23	12	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
24	12	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
25	13	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
26	13	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
27	14	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
28	14	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
29	15	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
30	15	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
31	16	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
32	16	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
33	17	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
34	17	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
35	18	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
36	18	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
37	19	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
38	19	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
39	20	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
40	20	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
41	21	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
42	21	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
43	22	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
44	22	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
45	23	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
46	23	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
47	24	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
48	24	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
49	25	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
50	25	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
51	26	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
52	26	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
53	27	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
54	27	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
55	28	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
56	28	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
57	29	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
58	29	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
59	30	Получено предложение	2026-05-20 17:40:34.249575	Новому клиенту автоматически назначено приветственное персональное предложение на 14 дней
60	30	Регистрация	2026-05-20 17:40:34.249575	Клиент зарегистрирован в программе лояльности
\.


--
-- Data for Name: customer_loyalty_accounts; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.customer_loyalty_accounts (account_id, customer_id, level_id, bonus_balance, total_spent, created_at, account_status) FROM stdin;
1	1	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
2	2	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
3	3	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
4	4	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
5	5	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
6	6	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
7	7	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
8	8	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
9	9	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
10	10	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
11	11	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
12	12	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
13	13	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
14	14	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
15	15	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
16	16	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
17	17	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
18	18	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
19	19	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
20	20	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
21	21	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
22	22	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
23	23	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
24	24	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
25	25	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
26	26	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
27	27	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
28	28	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
29	29	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
30	30	1	0.00	0.00	2026-05-20 17:40:34.249575	Активный
\.


--
-- Data for Name: customer_offers; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.customer_offers (offer_id, customer_id, promotion_id, assigned_at, valid_until, offer_status) FROM stdin;
1	1	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
2	2	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
3	3	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
4	4	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
5	5	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
6	6	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
7	7	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
8	8	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
9	9	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
10	10	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
11	11	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
12	12	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
13	13	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
14	14	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
15	15	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
16	16	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
17	17	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
18	18	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
19	19	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
20	20	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
21	21	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
22	22	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
23	23	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
24	24	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
25	25	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
26	26	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
27	27	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
28	28	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
29	29	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
30	30	1	2026-05-20 17:40:34.249575	2026-06-03 17:40:34.249575	Назначено
\.


--
-- Data for Name: customers; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.customers (customer_id, last_name, first_name, middle_name, phone, email, birth_date, gender, registration_date, status, updated_at) FROM stdin;
1	Иванов	Александр	Сергеевич	+79001234567	ivanov.a.s@mail.ru	1990-03-15	Мужской	2024-01-10 09:15:00	Активный	2026-05-20 17:40:34.249575
2	Смирнова	Мария	Ивановна	+79012345678	smirnova.m.i@yandex.ru	1985-07-22	Женский	2024-01-15 11:30:00	Активный	2026-05-20 17:40:34.249575
3	Кузнецов	Дмитрий	Александрович	+79023456789	kuznetsov.d.a@gmail.com	1993-11-05	Мужской	2024-02-03 14:00:00	Активный	2026-05-20 17:40:34.249575
4	Попова	Елена	Викторовна	+79034567890	popova.el.v@mail.ru	1988-05-30	Женский	2024-02-17 10:45:00	Активный	2026-05-20 17:40:34.249575
5	Васильев	Максим	Андреевич	+79045678901	vasiliev.m.a@rambler.ru	1995-09-12	Мужской	2024-03-01 08:20:00	Активный	2026-05-20 17:40:34.249575
6	Петрова	Ольга	Николаевна	+79056789012	petrova.ol.n@yandex.ru	1982-01-28	Женский	2024-03-14 15:10:00	Активный	2026-05-20 17:40:34.249575
7	Соколов	Андрей	Дмитриевич	+79067890123	sokolov.and.d@mail.ru	1991-06-17	Мужской	2024-04-05 09:55:00	Активный	2026-05-20 17:40:34.249575
8	Михайлова	Наталья	Сергеевна	+79078901234	mikhailova.nat@gmail.com	1987-12-03	Женский	2024-04-20 13:40:00	Активный	2026-05-20 17:40:34.249575
9	Новиков	Иван	Павлович	+79089012345	novikov.iv.p@mail.ru	1998-04-09	Мужской	2024-05-07 10:05:00	Активный	2026-05-20 17:40:34.249575
10	Фёдорова	Татьяна	Андреевна	+79091234567	fedorova.tat.a@yandex.ru	1979-08-25	Женский	2024-05-19 16:30:00	Активный	2026-05-20 17:40:34.249575
11	Морозов	Сергей	Владимирович	+79102345678	morozov.ser.v@mail.ru	1986-02-14	Мужской	2024-06-02 11:15:00	Активный	2026-05-20 17:40:34.249575
12	Волкова	Светлана	Михайловна	+79113456789	volkova.sv.m@gmail.com	1992-10-31	Женский	2024-06-18 09:00:00	Активный	2026-05-20 17:40:34.249575
13	Алексеев	Алексей	Иванович	+79124567890	alekseev.al.i@rambler.ru	1975-07-07	Мужской	2024-07-03 14:50:00	Активный	2026-05-20 17:40:34.249575
14	Лебедева	Юлия	Александровна	+79135678901	lebedeva.yu.a@mail.ru	1997-03-19	Женский	2024-07-22 08:35:00	Активный	2026-05-20 17:40:34.249575
15	Семёнов	Михаил	Евгеньевич	+79146789012	semenov.mi.e@yandex.ru	1983-11-11	Мужской	2024-08-06 12:00:00	Активный	2026-05-20 17:40:34.249575
16	Егорова	Ирина	Сергеевна	+79157890123	egorova.ir.s@gmail.com	1994-06-26	Женский	2024-08-15 10:25:00	Активный	2026-05-20 17:40:34.249575
17	Павлов	Николай	Владимирович	+79168901234	pavlov.nik.v@mail.ru	1980-09-04	Мужской	2024-01-28 17:00:00	Неактивный	2026-05-20 17:40:34.249575
18	Козлова	Екатерина	Андреевна	+79179012345	kozlova.ek.a@yandex.ru	1996-01-15	Женский	2024-09-10 09:45:00	Активный	2026-05-20 17:40:34.249575
19	Степанов	Владимир	Алексеевич	+79181234567	stepanov.vl.a@mail.ru	1989-05-08	Мужской	2024-09-25 11:20:00	Активный	2026-05-20 17:40:34.249575
20	Николаева	Дарья	Михайловна	+79192345678	nikolaeva.dar.m@gmail.com	2001-08-20	Женский	2024-10-01 14:15:00	Активный	2026-05-20 17:40:34.249575
21	Захаров	Артём	Николаевич	+79203456789	zakharov.art.n@mail.ru	1993-12-27	Мужской	2024-10-14 08:50:00	Активный	2026-05-20 17:40:34.249575
22	Зайцева	Алина	Дмитриевна	+79214567890	zaitseva.al.d@yandex.ru	1999-04-03	Женский	2024-10-28 15:30:00	Активный	2026-05-20 17:40:34.249575
23	Борисов	Евгений	Павлович	+79225678901	borisov.ev.p@rambler.ru	1977-07-14	Мужской	2024-02-10 10:00:00	Неактивный	2026-05-20 17:40:34.249575
24	Тарасова	Вера	Александровна	+79236789012	tarasova.ver.a@mail.ru	1984-10-02	Женский	2024-11-05 09:10:00	Активный	2026-05-20 17:40:34.249575
25	Григорьев	Павел	Сергеевич	+79247890123	grigoriev.pav.s@gmail.com	1991-02-18	Мужской	2024-11-19 13:55:00	Активный	2026-05-20 17:40:34.249575
26	Герасимова	Людмила	Ивановна	+79258901234	gerasimova.lud.i@yandex.ru	1972-06-09	Женский	2024-12-02 11:40:00	Активный	2026-05-20 17:40:34.249575
27	Чернов	Роман	Андреевич	+79269012345	chernov.rom.a@mail.ru	2000-03-24	Мужской	2024-12-16 08:15:00	Активный	2026-05-20 17:40:34.249575
28	Крылова	Полина	Евгеньевна	+79271234567	krylova.pol.e@gmail.com	1995-09-30	Женский	2025-01-08 10:30:00	Активный	2026-05-20 17:40:34.249575
29	Орлов	Кирилл	Максимович	+79282345678	orlov.kir.m@mail.ru	1988-11-16	Мужской	2024-03-22 16:45:00	Заблокирован	2026-05-20 17:40:34.249575
30	Андреева	Виктория	Николаевна	+79293456789	andreeva.vik.n@yandex.ru	2002-07-05	Женский	2025-02-14 12:20:00	Активный	2026-05-20 17:40:34.249575
\.


--
-- Data for Name: loyalty_levels; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.loyalty_levels (level_id, level_name, min_total_spent, bonus_percent) FROM stdin;
1	Базовый	0.00	0.00
2	Серебряный	10000.00	5.00
3	Золотой	15000.00	7.50
4	Платиновый	20000.00	10.00
\.


--
-- Data for Name: promotions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.promotions (promotion_id, promotion_type, promotion_name, description, start_date, end_date, bonus_multiplier, extra_bonus, required_level_id, is_active) FROM stdin;
1	Новый клиент	Приветственные бонусы	Автоматически назначается новому клиенту при регистрации. Действует 14 дней.	2026-01-01	2026-12-31	1.00	200.00	\N	t
2	День рождения	Подарок ко дню рождения	Автоматически назначается за 3 дня до дня рождения клиента. Действует 6 дней.	2026-01-01	2026-12-31	2.00	300.00	\N	t
3	Возврат клиента	Приглашение вернуться	Автоматически назначается активным клиентам, не совершавшим покупок от 30 до 60 дней. Действует 30 дней.	2026-01-01	2026-12-31	1.50	150.00	\N	t
4	Возврат покупки	Компенсация за возврат	Автоматически назначается клиенту после оформления возврата покупки. Действует 14 дней.	2026-01-01	2026-12-31	1.00	100.00	\N	t
\.


--
-- Data for Name: roles; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.roles (role_id, role_name) FROM stdin;
1	Администратор
2	Менеджер
3	Аналитик
\.


--
-- Data for Name: transactions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.transactions (transaction_id, transaction_type, original_transaction_id, customer_id, transaction_datetime, transaction_amount, bonus_used, paid_amount, bonus_accrued, transaction_channel, promotion_id, offer_id, comment, bonus_compensation_amount) FROM stdin;
\.


--
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.users (user_id, last_name, first_name, middle_name, login, password_hash, role_id, is_active, created_at, updated_at) FROM stdin;
1	Ильин	Константин	\N	admin	PBKDF2$100000$E9iG8dMGxb50cf5asE+ISQ==$xG/0bWlOBRYv4ceddYt9NnHuYPGYlg+KZk7db9KWoBc=	1	t	2026-05-20 17:26:52.694993	2026-05-20 17:26:52.841349
\.


--
-- Name: bonus_transactions_bonus_transaction_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.bonus_transactions_bonus_transaction_id_seq', 1, false);


--
-- Name: customer_activity_activity_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.customer_activity_activity_id_seq', 60, true);


--
-- Name: customer_loyalty_accounts_account_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.customer_loyalty_accounts_account_id_seq', 30, true);


--
-- Name: customer_offers_offer_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.customer_offers_offer_id_seq', 30, true);


--
-- Name: customers_customer_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.customers_customer_id_seq', 30, true);


--
-- Name: loyalty_levels_level_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.loyalty_levels_level_id_seq', 4, true);


--
-- Name: promotions_promotion_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.promotions_promotion_id_seq', 4, true);


--
-- Name: roles_role_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.roles_role_id_seq', 3, true);


--
-- Name: transactions_transaction_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.transactions_transaction_id_seq', 1, false);


--
-- Name: users_user_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.users_user_id_seq', 1, true);


--
-- Name: bonus_transactions bonus_transactions_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bonus_transactions
    ADD CONSTRAINT bonus_transactions_pkey PRIMARY KEY (bonus_transaction_id);


--
-- Name: customer_activity customer_activity_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_activity
    ADD CONSTRAINT customer_activity_pkey PRIMARY KEY (activity_id);


--
-- Name: customer_loyalty_accounts customer_loyalty_accounts_customer_id_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_loyalty_accounts
    ADD CONSTRAINT customer_loyalty_accounts_customer_id_key UNIQUE (customer_id);


--
-- Name: customer_loyalty_accounts customer_loyalty_accounts_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_loyalty_accounts
    ADD CONSTRAINT customer_loyalty_accounts_pkey PRIMARY KEY (account_id);


--
-- Name: customer_offers customer_offers_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_offers
    ADD CONSTRAINT customer_offers_pkey PRIMARY KEY (offer_id);


--
-- Name: customers customers_email_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customers
    ADD CONSTRAINT customers_email_key UNIQUE (email);


--
-- Name: customers customers_phone_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customers
    ADD CONSTRAINT customers_phone_key UNIQUE (phone);


--
-- Name: customers customers_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customers
    ADD CONSTRAINT customers_pkey PRIMARY KEY (customer_id);


--
-- Name: loyalty_levels loyalty_levels_level_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.loyalty_levels
    ADD CONSTRAINT loyalty_levels_level_name_key UNIQUE (level_name);


--
-- Name: loyalty_levels loyalty_levels_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.loyalty_levels
    ADD CONSTRAINT loyalty_levels_pkey PRIMARY KEY (level_id);


--
-- Name: promotions promotions_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.promotions
    ADD CONSTRAINT promotions_pkey PRIMARY KEY (promotion_id);


--
-- Name: roles roles_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (role_id);


--
-- Name: roles roles_role_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_role_name_key UNIQUE (role_name);


--
-- Name: transactions transactions_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.transactions
    ADD CONSTRAINT transactions_pkey PRIMARY KEY (transaction_id);


--
-- Name: users users_login_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_login_key UNIQUE (login);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (user_id);


--
-- Name: idx_bonus_transactions_account_datetime; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_bonus_transactions_account_datetime ON public.bonus_transactions USING btree (account_id, bonus_transaction_datetime);


--
-- Name: idx_customer_activity_customer_datetime; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_customer_activity_customer_datetime ON public.customer_activity USING btree (customer_id, activity_datetime);


--
-- Name: idx_customer_full_name; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_customer_full_name ON public.customers USING btree (last_name, first_name, middle_name);


--
-- Name: idx_customer_offers_customer_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_customer_offers_customer_status ON public.customer_offers USING btree (customer_id, offer_status);


--
-- Name: idx_customer_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_customer_status ON public.customers USING btree (status);


--
-- Name: idx_loyalty_accounts_level_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_loyalty_accounts_level_id ON public.customer_loyalty_accounts USING btree (level_id);


--
-- Name: idx_loyalty_accounts_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_loyalty_accounts_status ON public.customer_loyalty_accounts USING btree (account_status);


--
-- Name: idx_transactions_customer_datetime; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_transactions_customer_datetime ON public.transactions USING btree (customer_id, transaction_datetime);


--
-- Name: ux_transactions_one_return_per_purchase; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX ux_transactions_one_return_per_purchase ON public.transactions USING btree (original_transaction_id) WHERE (transaction_type = 'Возврат'::public.transaction_type_enum);


--
-- Name: customers trg_assign_new_customer_offer; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_assign_new_customer_offer AFTER INSERT ON public.customers FOR EACH ROW EXECUTE FUNCTION public.fn_assign_new_customer_offer();


--
-- Name: customers trg_create_loyalty_account_after_customer; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_create_loyalty_account_after_customer AFTER INSERT ON public.customers FOR EACH ROW EXECUTE FUNCTION public.fn_create_loyalty_account_after_customer();


--
-- Name: customers trg_customers_set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_customers_set_updated_at BEFORE UPDATE ON public.customers FOR EACH ROW EXECUTE FUNCTION public.fn_set_updated_at();


--
-- Name: transactions trg_prepare_transaction_before_insert; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_prepare_transaction_before_insert BEFORE INSERT ON public.transactions FOR EACH ROW EXECUTE FUNCTION public.fn_prepare_transaction_before_insert();


--
-- Name: transactions trg_process_transaction_after_insert; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_process_transaction_after_insert AFTER INSERT ON public.transactions FOR EACH ROW EXECUTE FUNCTION public.fn_process_transaction_after_insert();


--
-- Name: users trg_users_set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_users_set_updated_at BEFORE UPDATE ON public.users FOR EACH ROW EXECUTE FUNCTION public.fn_set_updated_at();


--
-- Name: customer_activity fk_activity_customer; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_activity
    ADD CONSTRAINT fk_activity_customer FOREIGN KEY (customer_id) REFERENCES public.customers(customer_id) ON DELETE SET NULL;


--
-- Name: bonus_transactions fk_bonus_account; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bonus_transactions
    ADD CONSTRAINT fk_bonus_account FOREIGN KEY (account_id) REFERENCES public.customer_loyalty_accounts(account_id) ON DELETE CASCADE;


--
-- Name: bonus_transactions fk_bonus_for_transaction; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bonus_transactions
    ADD CONSTRAINT fk_bonus_for_transaction FOREIGN KEY (transaction_id) REFERENCES public.transactions(transaction_id) ON DELETE SET NULL;


--
-- Name: customer_loyalty_accounts fk_loyalty_customer; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_loyalty_accounts
    ADD CONSTRAINT fk_loyalty_customer FOREIGN KEY (customer_id) REFERENCES public.customers(customer_id) ON DELETE CASCADE;


--
-- Name: customer_loyalty_accounts fk_loyalty_level; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_loyalty_accounts
    ADD CONSTRAINT fk_loyalty_level FOREIGN KEY (level_id) REFERENCES public.loyalty_levels(level_id) ON DELETE RESTRICT;


--
-- Name: customer_offers fk_offer_customer; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_offers
    ADD CONSTRAINT fk_offer_customer FOREIGN KEY (customer_id) REFERENCES public.customers(customer_id) ON DELETE CASCADE;


--
-- Name: customer_offers fk_offer_promotion; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.customer_offers
    ADD CONSTRAINT fk_offer_promotion FOREIGN KEY (promotion_id) REFERENCES public.promotions(promotion_id) ON DELETE RESTRICT;


--
-- Name: promotions fk_promotion_level; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.promotions
    ADD CONSTRAINT fk_promotion_level FOREIGN KEY (required_level_id) REFERENCES public.loyalty_levels(level_id) ON DELETE SET NULL;


--
-- Name: transactions fk_transactions_customer; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.transactions
    ADD CONSTRAINT fk_transactions_customer FOREIGN KEY (customer_id) REFERENCES public.customers(customer_id) ON DELETE SET NULL;


--
-- Name: transactions fk_transactions_offer; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.transactions
    ADD CONSTRAINT fk_transactions_offer FOREIGN KEY (offer_id) REFERENCES public.customer_offers(offer_id) ON DELETE SET NULL;


--
-- Name: transactions fk_transactions_original_transaction; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.transactions
    ADD CONSTRAINT fk_transactions_original_transaction FOREIGN KEY (original_transaction_id) REFERENCES public.transactions(transaction_id) ON DELETE RESTRICT;


--
-- Name: transactions fk_transactions_promotion; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.transactions
    ADD CONSTRAINT fk_transactions_promotion FOREIGN KEY (promotion_id) REFERENCES public.promotions(promotion_id) ON DELETE SET NULL;


--
-- Name: users fk_user_role; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT fk_user_role FOREIGN KEY (role_id) REFERENCES public.roles(role_id) ON DELETE RESTRICT;


--
-- PostgreSQL database dump complete
--

\unrestrict fMauuD4GN1rphib4nRLqkUrpRH7cFwd2l02DFgUP1cQqmEf8JfVL8galhaSNxC3

