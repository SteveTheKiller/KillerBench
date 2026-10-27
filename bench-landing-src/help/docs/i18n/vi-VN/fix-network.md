# Khắc phục sự cố mạng và internet

Dùng hướng dẫn này khi máy tính không có internet, không truy cập được máy chủ hoặc tên miền, hoặc liên tục mất kết nối. KillerBench lần lượt kiểm tra bộ điều hợp, địa chỉ, cổng mặc định, DNS và tường lửa, và chỉ gia hạn thuê địa chỉ hoặc khởi động dịch vụ khi bằng chứng mới chứng minh là cần thiết.

## Kiểm tra kết nối

1. Mở **Mạng** và chọn **Mạng**.
2. Để kiểm tra một mục cụ thể, hãy điền **Tra cứu tên máy chủ (tùy chọn)**, hoặc **Đích TCP (tùy chọn)** và **Cổng TCP**. Để trống để chỉ kiểm tra cấu hình cục bộ. Không có dữ liệu ứng dụng nào được gửi đi.
3. Đặt **Thời gian chờ cho mỗi lượt tra cứu hoặc kết nối (giây)** nếu mạng chậm.
4. Để **Tự động sửa các vấn đề đã xác nhận** tắt và nhấn **Chạy chẩn đoán**.
5. Đọc báo cáo. Báo cáo tách riêng địa chỉ bị thiếu, cổng mặc định bị thiếu hoặc không truy cập được, sự cố cấu hình DNS, cài đặt proxy, hồ sơ tường lửa không khớp và lần tra cứu hoặc kết nối thất bại, kèm bước tiếp theo cho từng trường hợp.

Phản hồi từ cổng mặc định không chứng minh có truy cập internet, và một lần tra cứu thất bại qua trình phân giải đã cấu hình không có nghĩa là trang web đang ngừng hoạt động.

## Sửa những gì báo cáo tìm thấy

1. Chạy lại **Mạng** với **Tự động sửa các vấn đề đã xác nhận** được bật. Công cụ có thể khởi động các dịch vụ DNS Client, DHCP Client, máy khách SMB, Network Location Awareness, Network List và IP Helper đang bật, và gia hạn thuê cho các bộ điều hợp DHCP vật lý đã kết nối nhưng thiếu địa chỉ IPv4 dùng được.
2. Kết nối có thể mất trong chốc lát khi gia hạn. Địa chỉ tĩnh và bộ điều hợp ảo không bao giờ bị thay đổi.
3. Với một tên bị lưu sai trong bộ nhớ đệm, hãy dùng thao tác xóa bộ nhớ đệm DNS cho đúng máy chủ đó thay vì xóa toàn bộ.

Tuyến đường, mục Hosts, máy chủ DNS, quy tắc tường lửa, nhà cung cấp Winsock và hồ sơ mạng luôn được giữ nguyên.

## Không dây, đăng nhập có dây và proxy

- **Mạng không dây** kiểm tra bộ điều hợp và WLAN AutoConfig, và có thể khởi động dịch vụ nếu nó đã dừng.
- **Xác thực có dây (802.1X)** làm tương tự cho Wired AutoConfig trên các mạng cần đăng nhập tại cổng.
- **Proxy tự động WinHTTP** hiển thị cài đặt proxy đã lưu cho máy và tài khoản hiện tại. KillerBench không bao giờ tải URL PAC hay thay đổi cài đặt proxy trong lúc chẩn đoán.

## Từ dòng lệnh

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Xem [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) và [dns-cache](cli.html#cmd-dns-cache).

## Liên quan

- [Mục Mạng](network.html)
- [Khắc phục sự cố in ấn](fix-printing.html)
- [Cách giữ an toàn khi thay đổi](safety.html)
