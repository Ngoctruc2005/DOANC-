# PRD v1.0 — Tourism App & CMS (TourismApp + TourismCMS)

> **Trạng thái:** Draft v1.0 · **Ngày:** 2026-04-06  
> **Phạm vi:** Bám sát 100% UI/code hiện có trong repo. Không thêm module ngoài scope.

---

## Mục lục

1. [Overview & Goals](#1-overview--goals)
2. [In-scope / Out-of-scope (MVP)](#2-in-scope--out-of-scope-mvp)
3. [Personas / Roles](#3-personas--roles)
4. [Kiến trúc hệ thống](#4-kiến-trúc-hệ-thống)
5. [User Stories](#5-user-stories)
6. [Functional Requirements (FR)](#6-functional-requirements-fr)
7. [Acceptance Criteria (Given-When-Then)](#7-acceptance-criteria-given-when-then)
8. [Non-functional Requirements](#8-non-functional-requirements)
9. [Data Requirements](#9-data-requirements)
10. [API Assumptions](#10-api-assumptions)
11. [Dependencies / Risks](#11-dependencies--risks)
12. [Open Questions](#12-open-questions)
13. [Future Enhancements](#13-future-enhancements)

---

## 1. Overview & Goals

**TourismApp** là ứng dụng di động (.NET MAUI – Android/iOS) dành cho khách du lịch, hiển thị bản đồ các điểm ăn uống (POI – Point of Interest), cho phép nghe thuyết minh audio, quét QR, và lưu yêu thích.

**TourismCMS** là hệ thống web quản trị (ASP.NET Core MVC) dành cho Admin và Chủ quán (poi_owner), cho phép quản lý danh sách POI, duyệt/từ chối đăng ký, và cung cấp REST API cho mobile app tiêu thụ.

**Mục tiêu MVP:**
- Mobile: Khách xem bản đồ POI, nghe audio thuyết minh, quét QR, lưu yêu thích, đổi ngôn ngữ UI.
- CMS: Admin quản lý toàn bộ POI và tài khoản chủ quán; Chủ quán đăng ký & quản lý POI của riêng mình; REST API phục vụ mobile.

---

## 2. In-scope / Out-of-scope (MVP)

### In-scope

| # | Module | Mô tả |
|---|--------|--------|
| 1 | **Auth (CMS)** | Đăng nhập, đăng ký tài khoản chủ quán, phân quyền `admin` / `poi_owner` |
| 2 | **Admin Dashboard** | Thống kê tổng quan (tổng POI, chờ duyệt, đã duyệt, chủ quán), biểu đồ truy cập |
| 3 | **Admin – POI Management** | Xem danh sách, tạo, sửa, xóa mềm, duyệt/từ chối POI; xem bản đồ Leaflet |
| 4 | **Admin – Owner Management** | Xem danh sách đăng ký chủ quán, duyệt/từ chối tài khoản |
| 5 | **Owner Dashboard** | Chủ quán xem dashboard, quản lý POI riêng, tạo POI mới (trạng thái "Chờ duyệt") |
| 6 | **REST API (POIs)** | `GET /api/pois` – trả danh sách POI đã duyệt; `GET /api/menus` – trả menu của POI |
| 7 | **Mobile – Map** | Bản đồ hiển thị marker POI, tìm kiếm, panel chi tiết, chỉ đường, yêu thích, audio |
| 8 | **Mobile – Favorites** | Danh sách POI yêu thích (in-memory), xóa yêu thích |
| 9 | **Mobile – QR Scan** | Quét mã QR bằng camera để định danh POI |
| 10 | **Mobile – Settings** | Chọn ngôn ngữ UI (vi/en/zh/ja/ko), lưu vào Preferences |
| 11 | **Mobile – Restaurant Detail** | Xem chi tiết POI: ảnh, tên, mô tả, menu, nút audio/chỉ đường/yêu thích |

### Out-of-scope (MVP)

- Thanh toán / đặt bàn / đặt món online
- Đánh giá / bình luận từ người dùng
- Hệ thống thông báo push
- Quản lý order (TotalOrders trong ViewBag là placeholder)
- Upload file ảnh/audio trực tiếp qua form (hiện là text input URL)
- Tích hợp mạng xã hội

---

## 3. Personas / Roles

| Role | Nơi dùng | Mô tả |
|------|----------|--------|
| **Admin** | TourismCMS (web) | Quản trị viên hệ thống. Có toàn quyền trên tất cả POI, duyệt/từ chối chủ quán và POI. Đăng nhập qua bảng `AdminUsers`. |
| **poi_owner (Chủ quán)** | TourismCMS (web) | Chủ sở hữu quán ăn. Đăng ký tài khoản, được Admin duyệt. Chỉ quản lý POI thuộc `OwnerId` của mình. |
| **Khách du lịch** | TourismApp (mobile) | Người dùng cuối, không cần đăng nhập. Duyệt bản đồ, nghe audio, lưu yêu thích. |

---

## 4. Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────────┐
│                      TourismCMS                             │
│  (ASP.NET Core MVC – Web + REST API)                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │  Admin UI    │  │  Owner UI    │  │  REST API        │  │
│  │  /Admin      │  │  /POIs       │  │  /api/pois       │  │
│  │  /POIs(admin)│  │  /Owner      │  │  /api/menus      │  │
│  └──────────────┘  └──────────────┘  └──────────────────┘  │
│                         │                     │             │
│              ApplicationDbContext (EF Core)   │             │
│                 SQL Server / FoodPOI DB       │             │
└─────────────────────────────────────────────────────────────┘
                                                │ HTTP JSON
┌───────────────────────────────────────────────▼─────────────┐
│                    TourismApp (.NET MAUI)                    │
│  Tab: MapPage | FavoritePage | QRPage | SettingsPage        │
│  Services: PoiApiService | AudioService | GeofenceService   │
│            FavoriteService | LocalizationService            │
└─────────────────────────────────────────────────────────────┘
```

**Database:** SQL Server (`FoodPOI`), kết nối qua EF Core. Bảng chính: `POIs`, `Menu`, `AdminUsers`, `Users`, `Roles`, `Categories`, `POI_Categories`, `VisitLog`, `AuditLogs`, `PoiOwnerRegistrations`.

---

## 5. User Stories

### 5.1 Module: Authentication (CMS)

| ID | Role | Story | Priority |
|----|------|-------|----------|
| AUTH-01 | Admin / poi_owner | Tôi muốn **đăng nhập** bằng username/password để truy cập hệ thống | P0 |
| AUTH-02 | poi_owner | Tôi muốn **đăng ký tài khoản** (username + password + confirmPassword) để trở thành chủ quán | P0 |
| AUTH-03 | Admin / poi_owner | Tôi muốn **đăng xuất** khỏi hệ thống | P0 |
| AUTH-04 | Admin / poi_owner | Khi truy cập trang bị chặn, tôi muốn thấy trang **Access Denied** rõ ràng | P1 |

### 5.2 Module: Admin – Dashboard & POI Management

| ID | Role | Story | Priority |
|----|------|-------|----------|
| ADM-01 | Admin | Tôi muốn xem **dashboard** với 4 thống kê (tổng POI, chờ duyệt, đã duyệt, chủ quán) | P0 |
| ADM-02 | Admin | Tôi muốn xem **danh sách POI đã duyệt** kèm bản đồ Leaflet với marker | P0 |
| ADM-03 | Admin | Tôi muốn xem **danh sách POI chờ duyệt** riêng biệt | P0 |
| ADM-04 | Admin | Tôi muốn **duyệt** một POI (chuyển từ "Chờ duyệt" → "Open") | P0 |
| ADM-05 | Admin | Tôi muốn **từ chối** (Reject) một POI – xóa cứng khỏi DB | P0 |
| ADM-06 | Admin | Tôi muốn **tạo mới** POI (tự động duyệt luôn, Status = "Đã duyệt") | P0 |
| ADM-07 | Admin | Tôi muốn **chỉnh sửa** thông tin POI bất kỳ | P1 |
| ADM-08 | Admin | Tôi muốn **xóa mềm** POI (chuyển Status → "Đã xóa") | P1 |
| ADM-09 | Admin | Tôi muốn xem **chi tiết** một POI | P1 |

### 5.3 Module: Admin – Owner Management

| ID | Role | Story | Priority |
|----|------|-------|----------|
| OWN-01 | Admin | Tôi muốn xem **danh sách yêu cầu đăng ký** tài khoản chủ quán (status = "pending") | P0 |
| OWN-02 | Admin | Tôi muốn **duyệt** tài khoản chủ quán (set `IsVerified = true`) | P0 |
| OWN-03 | Admin | Tôi muốn **từ chối** yêu cầu đăng ký chủ quán | P0 |

### 5.4 Module: Owner – POI Management

| ID | Role | Story | Priority |
|----|------|-------|----------|
| POI-01 | poi_owner | Tôi muốn xem **dashboard** với số liệu POI của mình | P0 |
| POI-02 | poi_owner | Tôi muốn xem **danh sách POI của tôi** (lọc theo OwnerId) | P0 |
| POI-03 | poi_owner | Tôi muốn **tạo POI mới** (Status tự động = "Chờ duyệt") | P0 |
| POI-04 | poi_owner | Tôi muốn **chỉnh sửa** POI của mình (không được sửa OwnerId) | P1 |
| POI-05 | poi_owner | Tôi muốn **xóa mềm** POI của mình (chuyển Status → "Đã xóa") | P1 |
| POI-06 | poi_owner | Tôi muốn xem **chi tiết** POI của mình | P1 |

### 5.5 Module: Mobile – Map & POI

| ID | Role | Story | Priority |
|----|------|-------|----------|
| MAP-01 | Khách | Tôi muốn **xem bản đồ** với các marker POI đã được duyệt | P0 |
| MAP-02 | Khách | Tôi muốn **tìm kiếm** POI theo tên và thấy kết quả dropdown | P0 |
| MAP-03 | Khách | Khi chọn marker/kết quả tìm kiếm, tôi muốn thấy **panel chi tiết** (tên, mô tả, menu, best seller) | P0 |
| MAP-04 | Khách | Tôi muốn **nghe audio thuyết minh** về POI (TTS hoặc file audio) | P0 |
| MAP-05 | Khách | Tôi muốn nhấn **Chỉ đường** để mở navigation đến POI | P1 |
| MAP-06 | Khách | Tôi muốn nhấn **Yêu thích** để lưu POI vào danh sách yêu thích | P1 |
| MAP-07 | Khách | Tôi muốn **đóng panel chi tiết** để quay về bản đồ | P1 |
| MAP-08 | Khách | Khi đến **gần một POI** (trong bán kính `Radius`), hệ thống tự nhận diện qua geofence | P1 |

### 5.6 Module: Mobile – Favorites

| ID | Role | Story | Priority |
|----|------|-------|----------|
| FAV-01 | Khách | Tôi muốn xem **danh sách POI yêu thích** đã lưu | P0 |
| FAV-02 | Khách | Tôi muốn **xóa một POI** khỏi danh sách yêu thích | P0 |
| FAV-03 | Khách | Tôi muốn nhấn vào một POI trong danh sách yêu thích để **xem chi tiết** | P1 |

### 5.7 Module: Mobile – QR Scan

| ID | Role | Story | Priority |
|----|------|-------|----------|
| QR-01 | Khách | Tôi muốn **quét mã QR** bằng camera để định danh POI | P1 |

### 5.8 Module: Mobile – Settings

| ID | Role | Story | Priority |
|----|------|-------|----------|
| SET-01 | Khách | Tôi muốn **chọn ngôn ngữ UI** từ danh sách (vi/en/zh/ja/ko) | P0 |
| SET-02 | Khách | Tôi muốn nhấn **Lưu cài đặt** để áp dụng ngôn ngữ mới | P0 |

---

## 6. Functional Requirements (FR)

### FR-AUTH: Authentication & Authorization

| ID | Yêu cầu |
|----|---------|
| FR-AUTH-01 | Hệ thống dùng Cookie Authentication (không dùng JWT session). Session không persistent (IsPersistent = false). |
| FR-AUTH-02 | Login kiểm tra `AdminUsers` trước (role = "admin"), sau đó kiểm tra `Users` (role = "poi_owner"). |
| FR-AUTH-03 | poi_owner phải có `IsVerified = true` mới được đăng nhập. |
| FR-AUTH-04 | Đăng ký chỉ tạo tài khoản `poi_owner`. Admin không được tạo qua form đăng ký. |
| FR-AUTH-05 | Username phải unique trong cả bảng `AdminUsers` lẫn `Users`. |
| FR-AUTH-06 | `[Authorize(Roles = "admin")]` bảo vệ toàn bộ AdminController và các action Pending/Approve/Reject của POIsController. |
| FR-AUTH-07 | `[Authorize(Roles = "poi_owner")]` bảo vệ OwnerController và các action POI khi role = poi_owner. |
| FR-AUTH-08 | poi_owner không được xem/sửa/xóa POI của người khác (kiểm tra OwnerId so với ClaimTypes.NameIdentifier). |

### FR-ADMIN: Admin Dashboard & POI

| ID | Yêu cầu |
|----|---------|
| FR-ADMIN-01 | Dashboard Admin hiển thị 4 thẻ thống kê: Tổng POI, Chờ duyệt, Đã duyệt, Chủ quán. Giá trị lấy thực từ DB (hiện đang là hardcode – xem Open Questions). |
| FR-ADMIN-02 | POIs/Index (Admin): chỉ hiển thị POI có Status != "Chờ duyệt" AND != "Đã xóa". |
| FR-ADMIN-03 | POIs/Pending: chỉ hiển thị POI có Status = "Chờ duyệt". |
| FR-ADMIN-04 | POIs/Approved: hiển thị POI Status != "Chờ duyệt" AND != "Đã xóa". |
| FR-ADMIN-05 | Approve: set Status = "Open". Reject: hard delete POI kèm cascade Menu, VisitLog, POI_Categories. |
| FR-ADMIN-06 | Admin tạo POI: Status tự động = "Đã duyệt". |
| FR-ADMIN-07 | Xóa mềm POI: set Status = "Đã xóa" thay vì DELETE FROM. |
| FR-ADMIN-08 | Bản đồ Leaflet trên trang Index chỉ hiển thị POI có tọa độ hợp lệ (Latitude != null, Longitude != null) và Status không phải "Chờ duyệt" hoặc "Đã xóa". |

### FR-OWNER: Owner POI Management

| ID | Yêu cầu |
|----|---------|
| FR-OWNER-01 | Owner chỉ xem POI thuộc OwnerId của chính mình (lọc theo ClaimTypes.NameIdentifier). |
| FR-OWNER-02 | Owner tạo POI: OwnerId gán từ claim, Status = "Chờ duyệt". |
| FR-OWNER-03 | Owner sửa POI: không cho thay đổi OwnerId. |
| FR-OWNER-04 | Owner xóa/sửa/xem chi tiết POI: kiểm tra OwnerId match, nếu không → HTTP 403 Forbid. |

### FR-API: REST API

| ID | Yêu cầu |
|----|---------|
| FR-API-01 | `GET /api/pois` trả về tất cả POI có Status != "Chờ duyệt" AND != "Đã xóa" dưới dạng JSON array. |
| FR-API-02 | `GET /api/menus` trả về tất cả Menu dưới dạng JSON array. Mobile tự lọc theo `poiid`. |
| FR-API-03 | API không yêu cầu authentication (AllowAnonymous cho mobile consumer). |
| FR-API-04 | API hỗ trợ HTTPS. Mobile dùng `HttpClientHandler` với certificate validation bỏ qua (dev mode). |

### FR-MOBILE-MAP: Bản đồ

| ID | Yêu cầu |
|----|---------|
| FR-MAP-01 | MapPage load danh sách POI từ `PoiApiService.GetAllPOIsAsync()` khi khởi động. |
| FR-MAP-02 | Mỗi POI có Latitude/Longitude được đặt marker trên bản đồ MAUI Maps. |
| FR-MAP-03 | SearchBar lọc POI theo Name (case-insensitive, partial match). Kết quả hiển thị dưới SearchBar dưới dạng CollectionView. |
| FR-MAP-04 | Khi chọn kết quả tìm kiếm hoặc tap marker: camera map di chuyển đến tọa độ POI, hiển thị `detailPanel`. |
| FR-MAP-05 | detailPanel hiển thị: tên, mô tả, best seller, danh sách menu (từ `GetMenusForPoiAsync`). |
| FR-MAP-06 | Nút "Thuyết minh": gọi `AudioService.Speak(description)` qua TTS. Nếu `AudioPath` có giá trị, ưu tiên phát file audio. |
| FR-MAP-07 | Nút "Chỉ đường": mở Maps native (Google Maps / Apple Maps) với tọa độ đích. |
| FR-MAP-08 | Nút "Yêu thích": thêm POI vào `FavoriteService.Favorites` (in-memory). |
| FR-MAP-09 | Nút "Đóng": ẩn `detailPanel`. |
| FR-MAP-10 | GeofenceService tính khoảng cách Haversine giữa vị trí user và tọa độ POI. Nếu distance ≤ Radius (m) → IsInside = true. |

### FR-MOBILE-FAV: Yêu thích

| ID | Yêu cầu |
|----|---------|
| FR-FAV-01 | FavoritePage load danh sách từ `FavoriteService.GetAll()`. |
| FR-FAV-02 | Nút Xóa gọi `FavoriteService.Remove(poi)` và refresh CollectionView. |
| FR-FAV-03 | Tap vào item trong danh sách → navigate đến `RestaurantDetailPage` với POI tương ứng. |

### FR-MOBILE-QR: Quét QR

| ID | Yêu cầu |
|----|---------|
| FR-QR-01 | QRPage khởi tạo camera preview trong `cameraHost`. |
| FR-QR-02 | Sau khi quét thành công, decode kết quả và điều hướng đến POI tương ứng. |

### FR-MOBILE-SETTINGS: Cài đặt

| ID | Yêu cầu |
|----|---------|
| FR-SET-01 | SettingsPage hiển thị Picker với 5 ngôn ngữ: vi, en, zh, ja, ko. |
| FR-SET-02 | Nhấn "Lưu cài đặt": gọi `LocalizationService.Instance.CurrentLanguage = selectedLang`, lưu vào `Preferences`. |
| FR-SET-03 | Toàn bộ UI text trong app đọc từ `LocalizationService[key]` (INotifyPropertyChanged). Khi đổi ngôn ngữ, binding tự update. |

---

## 7. Acceptance Criteria (Given-When-Then)

### AC-AUTH-01: Đăng nhập thành công (Admin)

| | |
|---|---|
| **Given** | User đã nhập đúng username và password của tài khoản Admin (có trong bảng AdminUsers) |
| **When** | User submit form POST /Auth/Login |
| **Then** | Cookie auth được set, user được redirect đến `/Admin/Index` (dashboard admin) |

### AC-AUTH-02: Đăng nhập thất bại – sai mật khẩu

| | |
|---|---|
| **Given** | User nhập username đúng nhưng password sai |
| **When** | User submit form POST /Auth/Login |
| **Then** | Trang Login được render lại với thông báo lỗi "Tên đăng nhập hoặc mật khẩu không đúng." Không set cookie. |

### AC-AUTH-03: Đăng nhập thất bại – chủ quán chưa được duyệt

| | |
|---|---|
| **Given** | User là poi_owner có `IsVerified = false` |
| **When** | User submit form POST /Auth/Login với đúng username/password |
| **Then** | Trang Login render lại với thông báo "Tài khoản của bạn chưa được admin duyệt." |

### AC-AUTH-04: Đăng ký chủ quán thành công

| | |
|---|---|
| **Given** | Username chưa tồn tại trong bảng Users và AdminUsers; password và confirmPassword khớp nhau |
| **When** | User submit form POST /Auth/Register |
| **Then** | User mới được insert vào bảng `Users` với Role = "poi_owner" và IsVerified = true (demo); trang hiển thị thông báo thành công |

### AC-ADM-01: Admin duyệt POI

| | |
|---|---|
| **Given** | Admin đã đăng nhập; POI có Status = "Chờ duyệt" đang hiển thị trong danh sách Pending |
| **When** | Admin nhấn nút "Duyệt" (POST /POIs/Approve?id={id}) |
| **Then** | Status của POI được cập nhật thành "Open" trong DB; POI biến khỏi danh sách Pending; API `/api/pois` trả về POI này cho mobile |

### AC-ADM-02: Admin từ chối POI

| | |
|---|---|
| **Given** | Admin đã đăng nhập; POI có Status = "Chờ duyệt" |
| **When** | Admin nhấn nút "Hủy" (POST /POIs/Reject?id={id}) và xác nhận dialog |
| **Then** | POI bị hard delete cùng toàn bộ Menus, VisitLogs và POI_Categories liên quan; không còn xuất hiện ở bất kỳ danh sách nào |

### AC-ADM-03: Admin xóa mềm POI đã duyệt

| | |
|---|---|
| **Given** | Admin đã đăng nhập; POI có Status = "Open" hoặc "Đã duyệt" |
| **When** | Admin nhấn "Delete" (POST /POIs/Delete?id={id}) và xác nhận |
| **Then** | Status POI được set thành "Đã xóa"; POI không hiển thị trên `/api/pois` và không xuất hiện trên map mobile; record vẫn còn trong DB |

### AC-OWNER-01: Chủ quán tạo POI mới

| | |
|---|---|
| **Given** | poi_owner đã đăng nhập; điền đầy đủ thông tin bắt buộc (Name, Latitude, Longitude, Address) |
| **When** | Owner submit POST /POIs/Create |
| **Then** | POI được insert với OwnerId = UserId từ claim, Status = "Chờ duyệt", CreatedAt = DateTime.Now; redirect về danh sách POI; POI **chưa** xuất hiện trên API public |

### AC-OWNER-02: Chủ quán không thể sửa POI của người khác

| | |
|---|---|
| **Given** | poi_owner A đăng nhập; POI với OwnerId = B |
| **When** | A cố gắng truy cập GET /POIs/Edit/{id của B} |
| **Then** | Hệ thống trả về HTTP 403 Forbidden; A không thể xem/sửa POI đó |

### AC-MAP-01: Hiển thị POI trên bản đồ mobile

| | |
|---|---|
| **Given** | TourismCMS đang chạy và có ít nhất 1 POI với Status = "Open", Latitude và Longitude hợp lệ |
| **When** | App khởi động MapPage và gọi `PoiApiService.GetAllPOIsAsync()` |
| **Then** | Các POI được hiển thị dưới dạng marker trên bản đồ MAUI Maps; không có POI nào với Status "Chờ duyệt" hoặc "Đã xóa" xuất hiện |

### AC-MAP-02: Tìm kiếm POI

| | |
|---|---|
| **Given** | MapPage đã load danh sách POI |
| **When** | User gõ ký tự vào SearchBar |
| **Then** | Dropdown `searchResultsBorder` hiển thị các POI khớp theo tên (partial match, không phân biệt hoa thường); nếu không có kết quả → dropdown ẩn |

### AC-MAP-03: Nghe audio thuyết minh

| | |
|---|---|
| **Given** | User đang xem detailPanel của một POI có Description |
| **When** | User nhấn nút "Thuyết minh" |
| **Then** | `AudioService.Speak(description)` được gọi và thiết bị phát giọng đọc văn bản (TTS) bằng ngôn ngữ mặc định |

### AC-FAV-01: Lưu và xóa yêu thích

| | |
|---|---|
| **Given** | User đang xem detailPanel của POI chưa có trong Favorites |
| **When** | User nhấn nút "Yêu thích" |
| **Then** | POI được thêm vào `FavoriteService.Favorites`; nếu user vào tab Favorites, POI xuất hiện trong danh sách |
| **And When** | User nhấn nút "Xóa" trên item trong FavoritePage |
| **And Then** | POI bị xóa khỏi danh sách yêu thích; CollectionView cập nhật ngay lập tức |

### AC-SET-01: Đổi ngôn ngữ

| | |
|---|---|
| **Given** | User đang ở SettingsPage, ngôn ngữ hiện tại là "vi" |
| **When** | User chọn "English (en)" trong Picker và nhấn "Lưu cài đặt" |
| **Then** | `LocalizationService.Instance.CurrentLanguage` được set = "en"; `Preferences.Get("language")` = "en"; toàn bộ text trên tất cả tab (tab bar title, button, label) chuyển sang tiếng Anh ngay lập tức |

---

## 8. Non-functional Requirements

### 8.1 Authentication & Authorization

- Mật khẩu hiện đang lưu plain text (xem Open Questions #1). Trước khi go-live production, phải hash bằng BCrypt hoặc tương đương.
- Cookie HttpOnly, Secure (production). SameSite = Lax.
- Mọi action viết (`POST /POIs/Create`, `Edit`, `Delete`, `Approve`, `Reject`) phải có `[ValidateAntiForgeryToken]`.

### 8.2 Validation

- CMS: Server-side ModelState validation cho tất cả form create/edit. Hiển thị lỗi validation qua `asp-validation-for`.
- Mobile: URL API cấu hình được qua `Preferences.Set("api_base_url", url)` (admin/dev override).
- Latitude phải trong [-90, 90]; Longitude trong [-180, 180].
- Name không được để trống (Required), tối đa 150 ký tự.
- Address tối đa 255 ký tự.

### 8.3 Error Handling

- CMS: Trả `NotFound()` (HTTP 404) khi POI không tồn tại. Trả `Forbid()` (HTTP 403) khi owner cố truy cập POI của người khác.
- Mobile: `PoiApiService` thử lần lượt các base URL trong danh sách; nếu tất cả fail, trả POI giả với tên "Lỗi API" và message lỗi để dev debug.
- Mobile: Timeout HTTP client = 15 giây.
- CMS: `DbUpdateConcurrencyException` trong Edit được catch; nếu record không còn tồn tại → `NotFound()`.

### 8.4 Logging

- CMS: `AuditLogs` table đã định nghĩa (LogId, UserId, Action, Timestamp). Cần implement ghi log cho các action quan trọng: Login, Approve POI, Reject POI, Approve Owner, Reject Owner.
- Mobile: Debug log qua `System.Diagnostics.Debug.WriteLine` cho API call result.

### 8.5 Performance (baseline)

- API `/api/pois` phải trả kết quả trong ≤ 2 giây với ≤ 500 POI records.
- Map mobile load marker trong ≤ 3 giây sau khi API trả về.
- TTS bắt đầu phát trong ≤ 1 giây sau khi nhấn nút.

### 8.6 Localization

- UI text mobile: Toàn bộ string phải đi qua `LocalizationService[key]`. Không hardcode string trực tiếp trong XAML (ngoại trừ các key đã có trong LocalizationService).
- Ngôn ngữ được persist qua app restart (lưu vào `Preferences`).

---

## 9. Data Requirements

### 9.1 Bảng POIs

| Field | Kiểu | Ràng buộc | Ghi chú |
|-------|------|-----------|---------|
| POIID | INT IDENTITY | PK | Auto-increment |
| OwnerId | INT | Nullable | FK → Users.Id. Null nếu Admin tạo |
| Name | NVARCHAR(150) | Required | Tên quán |
| Latitude | FLOAT | Nullable | -90 đến 90 |
| Longitude | FLOAT | Nullable | -180 đến 180 |
| Address | NVARCHAR(255) | Nullable | Địa chỉ đầy đủ |
| Description | NVARCHAR(MAX) | Nullable | Mô tả quán, dùng cho TTS |
| Thumbnail | VARCHAR(500) | Nullable | URL ảnh thumbnail |
| ImagePath | NVARCHAR(MAX) | Nullable | URL ảnh chính |
| AudioPath | NVARCHAR(MAX) | Nullable | URL file audio thuyết minh |
| Status | NVARCHAR(50) | Nullable | "Chờ duyệt" / "Open" / "Đã duyệt" / "Đã xóa" |
| Radius | FLOAT | Nullable | Bán kính geofence (mét) |
| CreatedAt | DATETIME | DEFAULT getdate() | Ngày tạo |

### 9.2 Bảng Menu

| Field | Kiểu | Ràng buộc | Ghi chú |
|-------|------|-----------|---------|
| MenuID | INT IDENTITY | PK | |
| POIID | INT | FK → POIs.POIID | |
| FoodName | NVARCHAR(150) | | Tên món (mobile bind từ `ItemName`) |
| Price | FLOAT | Nullable | Định dạng hiển thị: `{0:N0} đ` |
| Image | VARCHAR(500) | Nullable | URL ảnh món ăn |

### 9.3 Bảng Users

| Field | Kiểu | Ghi chú |
|-------|------|---------|
| Id | INT IDENTITY | PK |
| Username | NVARCHAR | Unique |
| Password | NVARCHAR | Plain text hiện tại – cần hash |
| Role | NVARCHAR | "poi_owner" (user tự đăng ký) |
| IsVerified | BIT | true sau khi Admin duyệt |

### 9.4 Bảng AdminUsers

| Field | Kiểu | Ghi chú |
|-------|------|---------|
| UserID | INT IDENTITY | PK |
| Username | NVARCHAR(100) | |
| Password | VARCHAR(255) | Plain text – cần hash |
| FullName | NVARCHAR(100) | |
| Email | VARCHAR(100) | |
| RoleID | INT | FK → Roles |

### 9.5 Bảng VisitLog

| Field | Kiểu | Ghi chú |
|-------|------|---------|
| VisitID | INT IDENTITY | PK |
| POIID | INT | FK → POIs |
| DeviceID | VARCHAR(100) | Định danh thiết bị |
| VisitTime | DATETIME | DEFAULT getdate() |

### 9.6 Status Lifecycle của POI

```
[Owner tạo]  →  "Chờ duyệt"
                    │
            ┌───────┴────────┐
         [Approve]        [Reject]
            │                │
          "Open"         Hard Delete
            │
         [Admin/Owner xóa]
            │
         "Đã xóa"
```

---

## 10. API Assumptions

*Mô tả interface. Không implement trong tài liệu này.*

### `GET /api/pois`

- **Auth:** None (AllowAnonymous)
- **Response 200:** `Array<POI>` – chỉ các POI với Status != "Chờ duyệt" AND != "Đã xóa"
- **Fields trả về:** `poiid`, `name`, `description`, `latitude`, `longitude`, `thumbnail`, `address`, `status`, `radius`, `imagePath`, `audioPath`, `createdAt`
- **Error:** 500 nếu DB không kết nối được

### `GET /api/menus`

- **Auth:** None (AllowAnonymous)
- **Response 200:** `Array<Menu>` – toàn bộ menu
- **Fields trả về:** `menuId`, `poiid`, `foodName`, `price`, `image`
- **Lưu ý:** Mobile tự filter theo `poiid` phía client (xem FR-API-02)

### *Endpoint chưa có trong code – cần xác nhận (xem Open Questions)*

| Endpoint | Mục đích | Priority |
|----------|----------|----------|
| `GET /api/pois/{id}` | Lấy chi tiết 1 POI theo ID (cho QR scan) | P1 |
| `GET /api/menus?poiid={id}` | Lấy menu lọc theo POI phía server | P2 |

---

## 11. Dependencies / Risks

| # | Loại | Mô tả | Mức độ | Giảm thiểu |
|---|------|--------|--------|------------|
| D1 | Tech | .NET MAUI target Android 10+ (net10.0-android) | - | Đảm bảo emulator/device đúng API level |
| D2 | Tech | MAUI Maps cần Google Maps API key (Android) / Apple Maps entitlement (iOS) | High | Cấu hình key trước khi build release |
| D3 | Tech | SQL Server LocalDB / SQLEXPRESS – connection string hardcode trong ApplicationDbContext | Medium | Di chuyển vào appsettings.json / environment variable |
| D4 | Security | Mật khẩu lưu plain text trong DB | Critical | Hash bằng BCrypt trước production |
| D5 | Network | Mobile dùng `ServerCertificateCustomValidationCallback = true` (bỏ qua SSL) | Critical | Chỉ dùng cho dev. Production phải dùng cert hợp lệ |
| D6 | Data | FavoriteService lưu in-memory – mất khi app bị kill | Medium | Xem Future Enhancements |
| D7 | API | Base URL API hardcode dev tunnel URL trong PoiApiService | Medium | Cho phép cấu hình qua Settings hoặc build config |
| R1 | Risk | Dashboard Admin dùng số hardcode (120, 15, 98, 25) thay vì query DB | High | Xem Open Questions #2 |
| R2 | Risk | Owner Dashboard dùng `OwnerId = 1` hardcode thay vì lấy từ claim | High | Xem Open Questions #3 |

---

## 12. Open Questions

| # | Câu hỏi | Ảnh hưởng | Owner |
|---|---------|-----------|-------|
| OQ-01 | **Password hashing**: Dùng BCrypt, PBKDF2 hay Argon2? Khi nào áp dụng? | AUTH-01, AUTH-02 | Dev Lead |
| OQ-02 | **Dashboard Admin**: 4 thẻ thống kê hiện hardcode (120/15/98/25). Có cần query thực từ DB không? Nếu có, query COUNT theo điều kiện nào? | ADM-01 | Product Owner |
| OQ-03 | **Owner Dashboard**: `OwnerId = 1` hardcode trong `OwnerController.MyRestaurants()`. Cần đổi sang `int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value)`. Xác nhận? | POI-01 | Dev |
| OQ-04 | **QR Code format**: Mã QR encode gì? `POIID` dạng số nguyên? URL đầy đủ? Cần spec rõ để implement `QRPage.xaml.cs`. | QR-01 | Product Owner |
| OQ-05 | **Audio file**: Khi `AudioPath` có giá trị, app nên stream URL trực tiếp hay download về trước? Định dạng file: MP3/WAV? | MAP-06 | Dev |
| OQ-06 | **Geofence trigger**: Khi `IsInside = true`, app làm gì? Tự hiện panel? Rung? Phát audio tự động? Logic chưa implement trong code. | MAP-08 | Product Owner |
| OQ-07 | **VisitLog ghi log khi nào?** Khi user mở detailPanel? Khi geofence trigger? `DeviceID` lấy từ đâu trên mobile? | FR-LOG | Dev |
| OQ-08 | **Menu endpoint server-side filter**: `GET /api/menus` hiện trả toàn bộ menu, mobile tự filter. Với dữ liệu lớn có cần thêm `GET /api/menus?poiid={id}` không? | FR-API-02 | Dev Lead |
| OQ-09 | **Status string nhất quán**: Code dùng cả "Đã duyệt" và "Open" cho trạng thái approved. Cần chuẩn hóa về một giá trị (đề xuất: "Open"). | FR-ADMIN-06 | Dev |
| OQ-10 | **AuditLog**: Bảng đã tạo nhưng chưa có code ghi log. Action nào cần ghi? Admin hay cả Owner? | FR-LOG | Product Owner |

---

## 13. Future Enhancements

> *Các mục dưới đây **không** thuộc MVP. Chỉ ghi nhận để không bị mất ý tưởng.*

- **Persistent Favorites (mobile):** Lưu yêu thích vào SQLite local hoặc `Preferences` để không mất khi restart app.
- **Upload ảnh/audio trực tiếp:** Form CMS hiện nhập URL text. Nâng cấp lên file upload với lưu trữ vào server hoặc cloud storage (Azure Blob / S3).
- **Password Change:** `AdminController.ChangePassword` đã code nhưng chưa có UI. Cần thêm form đổi mật khẩu.
- **Đánh giá & bình luận (Rating/Review):** Khách du lịch rate POI, admin kiểm duyệt.
- **Thông báo push:** Notify owner khi POI được duyệt/từ chối.
- **Đặt bàn / đặt món:** ViewBag.TotalOrders placeholder đã xuất hiện trong Owner Dashboard.
- **Bản đồ mobile filter theo category:** Hiện tất cả POI hiển thị cùng nhau; thêm filter theo `Categories`.
- **Localization POI content:** `PoiLocalization` model đã định nghĩa trong mobile nhưng chưa implement (TranslatedName, TranslatedDescription, AudioUrl theo language code).
- **Admin Analytics thực:** Biểu đồ "Thống kê truy cập" hiện dùng data tĩnh [12, 19, 3, 5, 10, 15]. Cần query từ bảng `VisitLog`.
- **Multi-image POI:** Hiện chỉ có `Thumbnail` + `ImagePath`. Nâng cấp lên gallery nhiều ảnh.


