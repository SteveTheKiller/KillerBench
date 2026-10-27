# Thiết bị

Thiết bị bao gồm máy in, âm thanh, máy ảnh, Bluetooth, thiết bị nhập và các dịch vụ Windows mà phần cứng gắn kèm dựa vào.

Hướng dẫn từng bước: [Khắc phục sự cố in ấn](fix-printing.html) và [Điều tra sự cố treo máy và màn hình xanh](investigate-crashes.html).

## Công cụ trong mục này

In ấn, âm thanh, máy ảnh, Bluetooth, thiết bị nhập và các dịch vụ đứng sau phần cứng gắn kèm.

### In ấn

Kiểm tra Print Spooler, các lần tạm dừng hàng đợi cục bộ, tác vụ in hiển thị, cài đặt ngoại tuyến và tình trạng do trình điều khiển báo cáo, sau đó kiểm kê đăng ký hàng đợi, trình điều khiển, cổng, điểm cuối Standard TCP/IP, bộ xử lý in và trình giám sát in. Chỉ đối chiếu các sự cố PrintService gần đây khi có đúng một danh tính hàng đợi hiện tại chính xác. Các điểm cuối Standard TCP/IP được gán nhận một lần thử kết nối có giới hạn mà không có dữ liệu ứng dụng, SNMP hay tác vụ in. Tự động sửa có thể khởi động bộ đệm in đang bật, chỉ tiếp tục đúng các hàng đợi bị tạm dừng có bằng chứng mới về tác vụ đang chờ, và tắt chế độ ngoại tuyến. Các lần tạm dừng có chủ ý khi nhàn rỗi vẫn chỉ để xem lại. Kết nối từ xa, chuỗi cộng đồng SNMP, đăng ký phần mở rộng và nội dung tác vụ được giữ nguyên. Một mã tình trạng hoặc kết nối thành công không xác lập danh tính máy in, vật tư, cơ cấu, việc hàng đợi chấp nhận, hiển thị thành công hay việc in thực tế.

### Âm thanh

Kiểm tra danh tính và định dạng dùng chung của thiết bị nhập và xuất đa phương tiện mặc định, trạng thái thiết bị và trình điều khiển âm thanh, Audio Endpoint Builder và Windows Audio. Tự động sửa khởi động các dịch vụ đang bật đã dừng và xác minh trạng thái đang chạy. Cài đặt khởi động, trình điều khiển, thiết bị đã chọn, chế độ độc quyền và mức âm lượng được giữ nguyên. Phát âm thanh, ghi âm và định tuyến âm thanh của ứng dụng vẫn chưa được kiểm thử.

### Hỗ trợ máy ảnh

Kiểm tra lỗi của máy ảnh và thiết bị hình ảnh, ID phần cứng, bằng chứng về trình điều khiển đã cài và tính khả dụng của Windows Camera Frame Server. Khôi phục tùy chọn chỉ khởi động dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc. Một ứng dụng đang yêu cầu truy cập máy ảnh có thể tiếp tục theo quyền hiện có. Cài đặt quyền riêng tư của máy ảnh, quyền, cấu hình thiết bị, chính sách, trình điều khiển và kiểu khởi động được giữ nguyên. Không đặt lại, không thay đổi quyền và không cài trình điều khiển. Việc xác minh chỉ bao gồm cấu hình thiết bị được báo cáo và trạng thái đang chạy của dịch vụ, không bao gồm chụp ảnh, quyền truy cập riêng tư hay hoạt động vật lý.

### Hỗ trợ Bluetooth

Kiểm tra lỗi thiết bị Bluetooth, ID phần cứng, bằng chứng về trình điều khiển đã cài và tính khả dụng của Bluetooth Support Service. Khôi phục tùy chọn chỉ khởi động dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc; các thiết bị đã ghép nối có thể kết nối lại. Ghép nối, cài đặt sóng vô tuyến, cấu hình thiết bị, chính sách và kiểu khởi động được giữ nguyên. Không đặt lại, không gỡ thiết bị và không cài trình điều khiển. Việc xác minh chỉ bao gồm cấu hình thiết bị được báo cáo và trạng thái đang chạy của dịch vụ, không bao gồm khám phá, ghép nối, âm thanh hay truyền dữ liệu.

### Hỗ trợ thẻ thông minh

Kiểm tra lỗi đầu đọc thẻ thông minh, ID phần cứng, bằng chứng về trình điều khiển đã cài và tính khả dụng của dịch vụ Smart Card. Khôi phục tùy chọn chỉ khởi động dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc. Các ứng dụng có thể tiếp tục phát hiện thẻ được cắm vào theo chính sách hiện có. Chứng chỉ, mã PIN, thông tin xác thực, cấu hình đầu đọc và thẻ, chính sách, trình điều khiển và kiểu khởi động được giữ nguyên. Không khởi tạo, không đăng ký, không thao tác PIN, không đặt lại đầu đọc và không cài trình điều khiển. Việc xác minh chỉ bao gồm cấu hình đầu đọc được báo cáo và trạng thái đang chạy của dịch vụ, không bao gồm phát hiện thẻ, xác thực hay truy cập của ứng dụng.

### Hỗ trợ sinh trắc học

Kiểm tra lỗi thiết bị sinh trắc học, ID phần cứng, bằng chứng về trình điều khiển đã cài và tính khả dụng của Windows Biometric Service. Khôi phục tùy chọn chỉ khởi động dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc. Các yêu cầu Windows Hello và ứng dụng hiện có có thể tiếp tục theo chính sách hiện tại. Mẫu sinh trắc học, dữ liệu Windows Hello, thông tin xác thực, cấu hình thiết bị, quyền, chính sách, trình điều khiển và kiểu khởi động được giữ nguyên. Không đăng ký, không xóa mẫu, không đặt lại cảm biến, không thay đổi quyền và không cài trình điều khiển. Việc xác minh chỉ bao gồm cấu hình cảm biến được báo cáo và trạng thái đang chạy của dịch vụ, không bao gồm thu nhận, đối sánh, đăng nhập hay truy cập của ứng dụng.

### Dịch vụ thiết bị giao tiếp người dùng (HID)

Kiểm tra tính khả dụng của Human Interface Device Service. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Các nút nóng, điều khiển đa phương tiện và tính năng HID khác hiện có có thể tiếp tục. Thiết bị, trình điều khiển, ánh xạ nút, cài đặt trợ năng, cài đặt nhập, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn thao tác nào về thiết bị, trình điều khiển, ánh xạ hay cài đặt nhập. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm hoạt động của nút, điều khiển hay ứng dụng.

### Dịch vụ liệt kê thiết bị di động

Kiểm tra tính khả dụng của Portable Device Enumerator Service. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Các ứng dụng hiện có có thể tiếp tục phát hiện và truy cập thiết bị di động đã kết nối. Thiết bị, trình điều khiển, tệp, thư viện, cài đặt đồng bộ hóa, quyền, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn thao tác nào về thiết bị, tệp, thư viện, đồng bộ hóa hay truyền dữ liệu. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm liệt kê, truy cập, đồng bộ hóa hay truyền dữ liệu.

### Thu nhận hình ảnh Windows (WIA)

Kiểm tra tính khả dụng của dịch vụ Windows Image Acquisition. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Các yêu cầu máy quét và thu nhận ảnh tĩnh hiện có có thể tiếp tục theo quyền ứng dụng hiện tại. Thiết bị, trình điều khiển, dữ liệu hình ảnh, quyền ứng dụng, cài đặt quyền riêng tư, cài đặt truyền, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn thao tác nào về thiết bị, trình điều khiển, hình ảnh, quyền, truyền hay thu nhận. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm hoạt động của máy quét, máy ảnh, việc truyền hay ứng dụng.

### Phát hiện phần cứng của trình bao

Kiểm tra bằng chứng Plug and Play có giới hạn về bộ điều khiển lưu trữ, đĩa và thiết bị di động cùng các tham chiếu trình điều khiển đã cài, sau đó kiểm tra tính khả dụng của dịch vụ Shell Hardware Detection. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Các thông báo AutoPlay và phần cứng mới cắm vào hiện có có thể tiếp tục theo chính sách hiện tại. Quan sát thiết bị và lịch sử được giữ lại không thể cho phép khôi phục. Thiết bị, trình điều khiển, ổ đĩa, điểm gắn kết, cài đặt AutoPlay, trình xử lý, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. Không có thao tác nào về thiết bị hay cài đặt được yêu cầu. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm việc gửi thông báo, mức sẵn sàng của phương tiện, truy cập ổ đĩa hay hoạt động vật lý của thiết bị.

### Dịch vụ cài đặt thiết bị

Kiểm tra tính khả dụng của Device Install Service. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Công việc cài đặt thiết bị do Windows yêu cầu hiện có có thể tiếp tục theo chính sách hiện tại. Thiết bị, trình điều khiển, gói, cấu hình, liên kết, cài đặt lớp, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn và không cài đặt gì. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm việc cài đặt hay hoạt động của thiết bị.

### Dịch vụ liên kết thiết bị

Kiểm tra lịch sử thiết lập thiết bị và Plug and Play gần đây, sau đó kiểm tra tính khả dụng của Device Association Service. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Công việc liên kết thiết bị do Windows yêu cầu hiện có có thể tiếp tục. Thiết bị đã ghép nối và liên kết, trình điều khiển, thông tin xác thực, siêu dữ liệu, quyền, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn thao tác nào về thiết bị, ghép nối, gỡ bỏ, đặt lại hay trình điều khiển. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm khám phá, ghép nối, liên kết hay hoạt động của thiết bị.

### Dịch vụ cải thiện hiển thị

Kiểm tra bằng chứng về bộ điều hợp đồ họa và màn hình, sau đó kiểm tra tính khả dụng của Display Enhancement Service. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Các yêu cầu độ sáng, màu sắc và cải thiện hiện có có thể tiếp tục. Cấu hình hiển thị, độ sáng, hồ sơ màu, cài đặt HDR, thiết bị, trình điều khiển, cài đặt nguồn điện, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. Không có cài đặt hiển thị nào bị thay đổi. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm hoạt động cải thiện, chất lượng hình ảnh hay hoạt động vật lý của màn hình.

### Dịch vụ quản lý nhập văn bản

Kiểm tra bằng chứng về bàn phím, chuột và thiết bị HID, sau đó kiểm tra tính khả dụng của Text Input Management Service. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Các yêu cầu bàn phím cảm ứng, chữ viết tay, biểu tượng cảm xúc và nhập văn bản nâng cao hiện có có thể tiếp tục. Từ điển, dữ liệu chữ viết tay, cài đặt ngôn ngữ và bàn phím, cài đặt trợ năng và quyền riêng tư, thiết bị, trình điều khiển, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. Không có văn bản nhập, thao tác gõ phím hay nội dung viết tay nào được thu thập. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm nhập văn bản, nhận dạng hay hoạt động của thiết bị.

### Dịch vụ bộ nhớ đệm phông chữ Windows

Kiểm tra tính khả dụng của Windows Font Cache Service. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Các ứng dụng hiện có có thể tiếp tục dùng bộ nhớ đệm phông chữ hiện tại. Phông chữ đã cài, tệp phông chữ, dữ liệu ứng dụng, tệp bộ nhớ đệm, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không cài hay gỡ phông chữ nào và không xóa hay dựng lại bộ nhớ đệm. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm hiển thị văn bản hay hoạt động của ứng dụng.

### Máy chủ nhà cung cấp khám phá chức năng

Kiểm tra tính khả dụng của Function Discovery Provider Host. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. Các ứng dụng hiện có có thể tiếp tục khám phá thiết bị mạng thông qua các nhà cung cấp đã cài. Hồ sơ mạng, chia sẻ, thiết bị, nhà cung cấp, quy tắc tường lửa, cài đặt khám phá, thông tin xác thực, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn thao tác nào về thiết bị, chia sẻ, nhà cung cấp, quy tắc tường lửa hay cài đặt khám phá. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm tình trạng nhà cung cấp, khám phá thiết bị hay kết nối.

### Xuất bản tài nguyên khám phá chức năng

Kiểm tra tính khả dụng của Function Discovery Resource Publication. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ khi dịch vụ đã dừng và được đặt khởi động tự động, sau khi kiểm tra phụ thuộc mới. Dịch vụ khởi động theo yêu cầu đã dừng chỉ được ghi nhận để tham khảo. PC này có thể trở nên có thể khám phá được thông qua cài đặt khám phá mạng và tường lửa hiện có. Hồ sơ mạng, chia sẻ, tài nguyên đã xuất bản, quy tắc tường lửa, cài đặt khám phá, thông tin xác thực, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. KillerBench không chọn thao tác nào về chia sẻ, tài nguyên, quy tắc tường lửa hay cài đặt khám phá. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm việc xuất bản, khả năng hiển thị, truy cập hay kết nối.
