# Veteriner Kliniği Randevu ve Hasta Takip Sistemi

- **Öğrenci:** B251210353 - Mustafa Emir Yılmaz
- **Ders Grubu:** 1.B
- **GitHub:** https://github.com/MustafaEmirY/VeterinerKlinikSistemi

## Proje Hakkında

Bu proje, Web Programlama dersi kapsamında **ASP.NET Core MVC (.NET 10 LTS)** kullanılarak geliştirilen bir **Veteriner Kliniği Randevu ve Hasta Takip Sistemi** uygulamasıdır. Sistem; veteriner kliniklerini ve sundukları hizmetleri, veteriner hekimlerin uzmanlık alanları ile müsaitlik saatlerini, evcil hayvanların muayene ve aşı geçmişini ve hayvan sahiplerinin çakışma kontrollü randevularını yönetmeyi hedefler. Ayrıca veritabanı raporlamaları için REST API uçları ve yapay zekâ asistanlarının doğrudan erişebileceği bir MCP (Model Context Protocol) sunucusu içerecektir.

## Kullanılan Teknolojiler

- ASP.NET Core MVC (.NET 10 LTS)
- C#
- SQL Server
- Entity Framework Core (Code First, Migration, LINQ)
- ASP.NET Core Identity
- `ModelContextProtocol.AspNetCore` (MCP C# SDK)
- Bootstrap 5
- HTML5, CSS3, JavaScript, jQuery

## Planlanan Modüller

- **Klinik ve Hizmet Yönetimi:** Klinik bilgileri, çalışma saatleri, hizmet türleri, süre ve ücret tanımlamaları.
- **Veteriner Hekim Yönetimi:** Hekim kayıtları, uzmanlık alanları, verebildiği hizmetler ve müsaitlik saatleri.
- **Evcil Hayvan ve Aşı Takibi:** Hayvan sahipleri, evcil hayvan kayıtları, aşı geçmişi/sonraki doz tarihleri ve muayene notları.
- **Randevu Sistemi:** Müsaitlik ve hizmet süresine göre çakışma kontrolü yapan, onay/red mekanizmalı randevu akışı.
- **REST API ve Raporlama:** LINQ sorguları ile filtreleme yapan, DTO döndüren API uçları.
- **MCP Sunucusu:** Yapay zekâ istemcileri için yetkilendirme korumalı ve ortak servis katmanını kullanan araçlar.

> *Not: Veritabanı diyagramı (ER), ekran görüntüleri, REST API ve MCP kullanım örnekleri geliştirme sürecinde bu dosyaya eklenecektir.*
