# Инструкция по настройке базы данных

## Требования
- PostgreSQL установлен и запущен
- .NET 8.0 SDK установлен

## Шаги настройки

### 1. Создание базы данных

Подключитесь к PostgreSQL и создайте базу данных:

```sql
CREATE DATABASE SimpleEcommerceDb;
```

### 2. Настройка строки подключения

#### Вариант 1: Использование User Secrets (рекомендуется для разработки)

Выполните в терминале в папке проекта:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=SimpleEcommerceDb;Username=postgres;Password=sysadmin"
```

Замените `your_password_here` на ваш пароль PostgreSQL.

#### Вариант 2: Использование appsettings.Development.json

Отредактируйте файл `appsettings.Development.json` и укажите правильную строку подключения:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SimpleEcommerceDb;Username=postgres;Password=your_password_here"
  }
}
```

### 3. Создание миграций

Выполните в терминале:

```bash
dotnet ef migrations add InitialCreate
```

### 4. Применение миграций к базе данных

```bash
dotnet ef database update
```

### 5. Запуск приложения

```bash
dotnet run
```

## Структура базы данных

После применения миграций будут созданы следующие таблицы:

- **Users** - пользователи системы
  - Id (PK)
  - Email (уникальный)
  - Password
  - FirstName
  - LastName
  - BirthDate
  - Balance

- **Transactions** - история транзакций
  - Id (PK)
  - UserId (FK)
  - Type (Пополнение/Покупка)
  - Amount
  - Description
  - CreatedAt
  - ProductId (опционально)
  - ProductName (опционально)

## Проверка подключения

После запуска приложения:
1. Зарегистрируйте нового пользователя
2. Проверьте, что пользователь создан в таблице Users
3. Проверьте, что создана транзакция регистрационного бонуса в таблице Transactions
