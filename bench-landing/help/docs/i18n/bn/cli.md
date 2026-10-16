# কমান্ড লাইন

`killerbench-cli` PowerShell বা একটি রিমোট কনসোল থেকে ডেস্কটপ অ্যাপের মতো একই Engine চালায়। কম্পিউটার পরিবর্তন করতে পারে এমন প্রতিটি কমান্ড ডিফল্টভাবে প্রিভিউ দেখায় এবং কাজ করার আগে `--execute`-এর মতো একটি স্পষ্ট ফ্ল্যাগ দরকার। কমান্ডগুলো JSON প্রমাণ স্ট্যান্ডার্ড আউটপুটে এবং অগ্রগতি স্ট্যান্ডার্ড এররে প্রিন্ট করে।

ধাপে ধাপে নির্দেশিকা: [স্ক্রিপ্ট থেকে KillerBench চালান](use-the-cli.html)।

এক্সিট কোড 0 মানে প্রিভিউ বা যাচাইকৃত ফলাফল সফল হয়েছে, 1 মানে স্টার্টআপ, ইনপুট বা জার্নালের ত্রুটি, 2 মানে আর্গুমেন্ট চেনা যায়নি, 3 মানে অবরুদ্ধ, ব্যর্থ বা অযাচাইকৃত, 4 মানে একটি ওয়ার্কফ্লোর যাচাইয়ের আগে রিস্টার্ট দরকার, এবং 130 মানে আপনি এটি থামিয়েছেন।

নিজের কম্পিউটারে এই তালিকা প্রিন্ট করতে কোনো আর্গুমেন্ট ছাড়া `killerbench-cli` চালান।

## আপডেট ও সার্ভিসিং

- `bits-job-review JOB_GUID [--all-users] [--observe]`: সঠিক ট্রান্সফার পর্যালোচনা বা দুই-স্ন্যাপশট পর্যবেক্ষণ; কোনো পুনঃচেষ্টা নেই।
- `bits-jobs [--all-users]`: BITS কাজের অবস্থা, মালিক ও বাইটের সংখ্যা; কোনো ট্রান্সফার পরিবর্তন নেই।
- `bits-retry JOB_GUID [--execute]`: ডিফল্টভাবে প্রিভিউ; স্পষ্ট সঠিক-কাজ পুনঃচেষ্টা, অ্যাপ্লিকেশন সমাপ্তির কোনো দাবি নেই।
- `bits-retry-recover JOB_GUID`: দুটি শুধু-পড়া স্ন্যাপশট; আরেকটি পুনঃচেষ্টা ছাড়াই স্থিতিশীল ফলাফল আর্কাইভ করে।
- `component-repair-source`: ব্যবস্থাপিত DISM মেরামত-উৎস নীতি ও বিরোধ; কোনো পরিবর্তন নেই।
- `delivery-optimization`: শুধু-পড়া ডেলিভারি অপটিমাইজেশন সার্ভিসের অবস্থা; JSON প্রমাণ।
- `delivery-optimization-activity`: গোপনীয়তা-সীমিত বর্তমান কাজের সংখ্যা ও মোট বাইট; কোনো ট্রান্সফার পরিচয় বা পরিবর্তন নেই।
- `delivery-optimization-policy`: শুধু-পড়া ক্লাসিক ও Policy CSP সেটিংস; সংবেদনশীল মান ও প্রোভাইডারের পরিচয় বাদ।
- `feature-service-events`: Time, Search, WMI, Event Log ও অডিও সার্ভিসের সঠিক ব্যর্থতার ইতিহাস পড়ে।
- `feature-update-policy`: শুধু-পড়া লক্ষ্য রিলিজ, স্থগিতকরণ, বিরতি ও সুরক্ষা নীতি; JSON প্রমাণ।
- `pending-file-operations`: শুধু-পড়া গোপনীয়তা-সীমিত মুলতুবি ফাইল অপারেশনের সারাংশ; JSON প্রমাণ।
- `restart-check`: শুধু-পড়া মুলতুবি রিস্টার্টের উৎস; JSON প্রমাণ।
- `servicing-logs`: সীমিত সর্বশেষ CBS, সাধারণ-টেক্সট ও সংকুচিত CBS আর্কাইভ, DISM ও Windows Setup লগ বিশ্লেষণ; কোনো মেরামত নেই।
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: শুধু-পড়া সার্ভিসিং কার্যক্রমের স্ন্যাপশট।
- `servicing-service-events`: শুধু-পড়া উইন্ডোজ আপডেট সার্ভিসিং চেইনের সঠিক ব্যর্থতার ইতিহাস।
- `servicing-service-states`: ছয়-সার্ভিসের উইন্ডোজ আপডেট সার্ভিসিং চেইনের শুধু-পড়া বর্তমান অবস্থা।
- `setup-code DECIMAL_OR_0xHEX`: নথিভুক্ত Windows Setup ফলাফল-কোড রেফারেন্স; কোনো কম্পিউটার পরিদর্শন বা মেরামত নেই।
- `update-applicability`: শুধু-পড়া Windows সংস্করণ, বিল্ড ও আর্কিটেকচারের প্রমাণ।
- `update-cache`: সীমিত শুধু-পড়া SoftwareDistribution ক্যাশের মোট পরিমাণ; কোনো ফাইলের নাম নেই।
- `update-code DECIMAL_OR_0xHEX`: নথিভুক্ত উইন্ডোজ আপডেট ফলাফল-কোড রেফারেন্স; কোনো কম্পিউটার পরিদর্শন বা মেরামত নেই।
- `update-context`: শুধু-পড়া বর্তমান এলিভেশন ও ডেস্কটপ-সেশনের প্রমাণ; কোনো অ্যাকাউন্ট শনাক্তকারী নেই।
- `update-engine-files`: শুধু-পড়া নেটিভ উইন্ডোজ আপডেট ইঞ্জিনের উপস্থিতি, সংস্করণ ও স্বাক্ষরের প্রমাণ।
- `update-events`: শেষ সাত দিনের শুধু-পড়া Windows Update Client ইতিহাস; কাঁচা প্রমাণসহ JSON।
- `update-followup DECIMAL_OR_0xHEX`: একটি নথিভুক্ত উইন্ডোজ আপডেট ফলাফল কোড দিয়ে নির্বাচিত নতুন শুধু-পড়া ডায়াগনস্টিক্স।
- `update-history`: শুধু-পড়া সর্বশেষ 100টি উইন্ডোজ আপডেট অপারেশনের ফলাফল; JSON প্রমাণ।
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: `--select` পুনরাবৃত্তি করুন; ডিফল্টভাবে প্রিভিউ।
- `update-policy`: শুধু-পড়া ব্যবস্থাপিত উইন্ডোজ আপডেট নীতি; JSON প্রমাণ।
- `update-policy-surfaces`: শুধু-পড়া ক্লাসিক ও Policy CSP কনফিগারেশন-ক্ষেত্রের প্রমাণ; মান ও প্রোভাইডারের পরিচয় বাদ।
- `update-recover`: একটি মুলতুবি আপডেট পরীক্ষা করে; কখনো ইনস্টল বা পুনঃচেষ্টা করে না।
- `update-restart-policy`: শুধু-পড়া কমপ্লায়েন্সের সময়সীমা, অতিরিক্ত সময়, রিস্টার্ট দমন ও বিজ্ঞপ্তির আচরণ।
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: JSON অনুসন্ধান ও মিল; কোনো ইনস্টলেশন নেই।
- `update-service-events`: শুধু-পড়া Windows Update সার্ভিসের সঠিক ব্যর্থতার ইতিহাস।
- `update-service-files`: শুধু-পড়া Windows Update সার্ভিস DLL-এর সঠিক নিবন্ধন ও স্বাক্ষরের প্রমাণ।
- `update-services`: শুধু-পড়া Windows Update সার্ভিসের নিবন্ধন; JSON প্রমাণ।
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: নতুন অনুসন্ধান-নির্ভর; ডিফল্টভাবে প্রিভিউ; পুরোনো স্টোর রেখে দেয়।
- `update-store-reset-recover`: একটি প্রস্তুত স্টোর রিসেট পুনরাবৃত্তি না করে মিলিয়ে দেখে।
- `windows-capabilities`: শুধু-পড়া অনলাইন Windows সক্ষমতার অবস্থা; JSON ফলাফল।
- `windows-update-task`: শুধু-পড়া সঠিক Scheduled Start টাস্কের অবস্থা ও রানটাইম ইতিহাস; কোনো টাস্ক চালানো বা আপডেট অপারেশন নেই।

## BitLocker ও TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: ডিফল্টভাবে প্রিভিউ; একটি এনক্রিপ্ট করা ডেটা ভলিউমের জন্য স্বয়ংক্রিয় আনলক চালু করে।
- `bitlocker-auto-unlock-recover`: একটি বাধাপ্রাপ্ত স্বয়ংক্রিয় আনলক অনুরোধ পুনরাবৃত্তি না করে যাচাই করে।
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: ডিফল্টভাবে প্রিভিউ; কেবল নতুন পুনরুদ্ধার ব্যাকআপ ফাইল।
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: একটি বাধাপ্রাপ্ত ব্যাকআপ আবার না লিখে যাচাই করে।
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: ডিফল্টভাবে প্রিভিউ; ডিক্রিপশন শুরু করে, যেমন প্রয়োজনীয় পদ্ধতিতে আবার এনক্রিপ্ট করার আগে।
- `bitlocker-decrypt-recover`: একটি বাধাপ্রাপ্ত ডিক্রিপশন অনুরোধ পুনরাবৃত্তি না করে যাচাই করে।
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: ডিফল্টভাবে প্রিভিউ; বিদ্যমান TPM ও পুনরুদ্ধার সুরক্ষক দরকার।
- `bitlocker-encrypt-recover`: অনুরোধ পুনরাবৃত্তি না করে মুলতুবি রূপান্তরের অবস্থা যাচাই করে।
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: ডিফল্টভাবে প্রিভিউ; একটি বিদ্যমান পুনরুদ্ধার পাসওয়ার্ড Active Directory বা Microsoft Entra ID-তে ব্যাকআপ করে।
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: কেবল বিকল্পের প্রিভিউ।
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: ডিফল্টভাবে প্রিভিউ; একটি TPM বা পুনরুদ্ধার-পাসওয়ার্ড সুরক্ষক যোগ করে; পাসওয়ার্ড কখনো প্রিন্ট হয় না।
- `bitlocker-protector-recover`: একটি বাধাপ্রাপ্ত সুরক্ষক অনুরোধ পুনরাবৃত্তি না করে যাচাই করে।
- `bitlocker-protectors DRIVE:`: কেবল সুরক্ষকের পরিচয় ও ধরন; কোনো পুনরুদ্ধার পাসওয়ার্ড নেই।
- `bitlocker-resume DRIVE: [--execute]`: ডিফল্টভাবে প্রিভিউ; কেবল সম্পূর্ণ এনক্রিপ্ট করা স্থগিত সঠিক ভলিউম।
- `bitlocker-resume-recover`: অনুরোধ পুনরাবৃত্তি না করে মুলতুবি সুরক্ষার অবস্থা যাচাই করে।

## নিরাপত্তা ও গোপনীয়তা

- `certificate-chains`: ব্যক্তিগত সার্টিফিকেটের জন্য শুধু-পড়া স্থানীয় কেবল-ক্যাশ শৃঙ্খল বিশ্বাস; কোনো বাতিলকরণ বা ডাউনলোড নেই।
- `defender-overview [--report]`: শুধু-পড়া সুরক্ষা, স্ক্যানের সময় ও সামগ্রিক হুমকির সারসংক্ষেপ; কোনো কাজ নেই।
- `defender-protection [--report]`: শুধু-পড়া সুরক্ষার পছন্দ ও স্বীকৃত নীতির ক্ষেত্র; সংবেদনশীল বিবরণ বাদ; কোনো পরিবর্তন নেই।
- `defender-scan Quick|Full [--execute] [--report]`: ডিফল্টভাবে প্রিভিউ; একটি স্পষ্ট সুরক্ষিত Microsoft Defender স্ক্যান।
- `defender-scan-history [--report]`: শুধু-পড়া দ্রুত ও সম্পূর্ণ স্ক্যানের সময় ও বিলম্বিত অবস্থা; সংবেদনশীল বিবরণ বাদ; কোনো স্ক্যান নেই।
- `defender-scan-recover [--report]`: আরেকটি স্ক্যান শুরু না করে শুধু-পড়া মিলিয়ে দেখা।
- `defender-threats [--report]`: সামগ্রিক হুমকির ইতিহাস, বর্তমান সক্রিয় ফ্ল্যাগ ও তীব্রতার আইডি; সংবেদনশীল বিবরণ বাদ; কোনো কাজ নেই।
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: ডিফল্টভাবে প্রিভিউ; কেবল অব্যবস্থাপিত স্থানীয় প্রোফাইল।
- `firewall-enable-recover`: একটি মুলতুবি পরিবর্তন পুনরাবৃত্তি না করে পরীক্ষা করে।
- `firewall-events`: সীমিত Windows Firewall সতর্কতা, ত্রুটি ও গুরুতর কার্যক্রমের ইতিহাস পড়ে।
- `firewall-logging`: লগ না খুলে Domain, Private ও Public ট্রাফিক লগিং কনফিগারেশন পড়ে।
- `firewall-rules`: গোপনীয়তা-সীমিত কার্যকর নিয়ম ও নীতি-উৎসের সংখ্যা পড়ে।
- `firewall-service-events`: BFE ও Windows Defender Firewall-এর সঠিক Service Control Manager ব্যর্থতার ইতিহাস পড়ে।
- `privacy-clipboard-history on|off [--execute]`: ডিফল্টভাবে প্রিভিউ; নীতি সংরক্ষণসহ সঠিক বর্তমান-ব্যবহারকারীর মান।
- `privacy-clipboard-history-recover`: একটি বাধাপ্রাপ্ত পরিবর্তন পুনরাবৃত্তি না করে আবার পরীক্ষা করে।
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: আগের মানের সুরক্ষিত পুনরুদ্ধার।
- `privacy-plan --select Camera --select ClipboardSync`: কেবল নির্বাচন পর্যালোচনা; কোনো সেটিং পরিবর্তিত হয় না।
- `privacy-setting KEY on|off [--execute]`: ডিফল্টভাবে প্রিভিউ; নীতি-সংরক্ষণকারী বর্তমান-ব্যবহারকারীর পরিবর্তন; অবৈধ কী সমর্থিত পছন্দগুলোর তালিকা দেখায়।
- `privacy-setting-recover KEY`: একটি বাধাপ্রাপ্ত পরিবর্তন পুনরাবৃত্তি না করে আবার পরীক্ষা করে।
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: সঠিক মানের সুরক্ষিত পুনরুদ্ধার।
- `security-check`: শুধু-পড়া TPM, Secure Boot, ভার্চুয়ালাইজেশন সুরক্ষা ও UAC নীতি; JSON প্রমাণ।

## সার্ভিস

- `service-binaries`: শুধু-পড়া স্থানীয় সার্ভিস বাইনারির উপস্থিতি ও Authenticode অবস্থা; পাথ বাদ এবং অ-স্থানীয় পাথে যোগাযোগ করা হয় না।
- `service-check NAME [NAME ...] | service-check --all`: শুধু-পড়া সমর্থিত সার্ভিসের অবস্থা ও নির্ভরতার প্রমাণ।
- `service-dependencies`: শুধু-পড়া সরাসরি নির্ভরতার অখণ্ডতা পর্যালোচনা; কোনো সার্ভিস পরিবর্তন নেই।
- `service-dlls`: শুধু-পড়া নিবন্ধিত সার্ভিস DLL-এর উপস্থিতি ও Authenticode অবস্থা; পাথ বাদ এবং অ-স্থানীয় পাথে যোগাযোগ করা হয় না।
- `service-events`: শুধু-পড়া সার্ভিস ব্যর্থতার ইতিহাস; কাঁচা প্রমাণসহ JSON।
- `time-events`: JSON প্রমাণসহ শুধু-পড়া সাম্প্রতিক Windows Time ঘটনার ইতিহাস।
- `service-logons`: শুধু-পড়া সার্ভিস অ্যাকাউন্ট-শ্রেণি ও ডেস্কটপ-মিথস্ক্রিয়া পর্যালোচনা; অ্যাকাউন্টের পরিচয় বাদ।
- `service-path-quoting`: শুধু-পড়া সার্ভিস এক্সিকিউটেবল পাথের উদ্ধৃতিচিহ্ন পর্যালোচনা; পূর্ণ পাথ ও আর্গুমেন্ট বাদ।
- `service-recovery-policy`: শুধু-পড়া ইনস্টল করা সার্ভিসের পুনরুদ্ধার কাজের সারাংশ; কমান্ড ও রিবুট বার্তা বাদ।
- `service-startup-config`: শুধু-পড়া স্টার্টআপ-মোডের সামঞ্জস্য পর্যালোচনা; কোনো সার্ভিস পরিবর্তন নেই।
- `service-transitions`: মুলতুবি সার্ভিস অবস্থার শুধু-পড়া পুনরাবৃত্ত পর্যবেক্ষণ; কোনো সার্ভিসের কাজ নেই।
- `service-triggers`: শুধু-পড়া ইনস্টল করা সার্ভিসের ট্রিগার সারাংশ; ট্রিগারের পেলোড বাদ; কোনো পরিবর্তন নেই।

## স্টোরেজ, ফাইল ও ব্যাকআপ

- `backup-events`: VSS ও volsnap ইতিহাস; JSON প্রমাণ; কোনো পরিবর্তন নেই।
- `backup-service-events`: VSS, সফটওয়্যার শ্যাডো প্রোভাইডার, Windows Backup ও ব্লক-লেভেল ইঞ্জিনের সঠিক ব্যর্থতার ইতিহাস পড়ে।
- `backup-summary`: স্থানীয় Windows Backup অপারেশনের সারাংশ; কোনো ব্যাকআপ পরিবর্তন নেই।
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: ডিফল্টভাবে প্রিভিউ; রিসাইকেল বিন সব-বা-কিছুই-না এবং বয়স প্রযোজ্য নয়।
- `disk-cleanup-task`: শুধু-পড়া সঠিক টাস্কের অবস্থা ও রানটাইম ইতিহাস; কোনো টাস্ক চালানো বা পরিষ্কার নেই।
- `disk-diagnostic`: সঠিক ডিস্ক ডায়াগনস্টিক টাস্ক ও ঘটনার ইতিহাস মিলিয়ে দেখে; কোনো টাস্ক বা ডিস্কের কাজ নেই।
- `disk-diagnostic-tasks`: শুধু-পড়া সঠিক সংগ্রাহক ও সমাধানকারী টাস্কের ইতিহাস; কোনো টাস্ক বা ডিস্কের কাজ নেই।
- `disk-quotas`: শুধু-পড়া প্রতি-ভলিউম কোটার ডিফল্ট ও অবস্থা; ব্যবহারকারীর পরিচয় বাদ; কোনো পরিবর্তন নেই।
- `file-history-events`: গোপনীয়তা-প্রক্ষিপ্ত File History সতর্কতা ও ত্রুটির সংখ্যা; কোনো পরিবর্তন নেই।
- `file-history-status`: বর্তমান অ্যাকাউন্টের File History সুরক্ষার অবস্থা; কোনো পরিবর্তন নেই।
- `search-index-rebuild [--execute]`: ডিফল্টভাবে প্রিভিউ; আগের ইনডেক্স রেখে দেয় এবং নতুন অবস্থা যাচাই করে।
- `search-index-rebuild-recover`: মেরামতের কমান্ড পুনরাবৃত্তি না করে শুধু-পড়া মিলিয়ে দেখা।
- `storage-check DRIVE_LETTER`: শুধু-পড়া নির্দিষ্ট NTFS পরীক্ষা; JSON ফলাফল ও লাইভ অগ্রগতি।
- `storage-events`: শুধু-পড়া স্টোরেজ ইতিহাস; কাঁচা প্রমাণসহ JSON।
- `storage-jobs`: শুধু-পড়া সামগ্রিক Storage মডিউলের কাজের অবস্থা ও অগ্রগতি; JSON; কোনো পরিবর্তন নেই।
- `storage-reliability`: শুধু-পড়া তাপমাত্রা, ক্ষয় ও I/O কাউন্টার; JSON; কোনো মেরামত নেই।
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: ডিফল্টভাবে প্রিভিউ; চালানোর জন্য ব্যাকআপ নিশ্চিতকরণ দরকার; সিস্টেম ভলিউম রিস্টার্টের সময় নির্ধারিত হয়।
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: কেবল প্রিভিউ; কোনো মেরামত বা সময় নির্ধারণ নেই।
- `storage-repair-recover`: একটি সম্পন্ন বা নির্ধারিত ফাইল সিস্টেম মেরামত পুনরাবৃত্তি না করে যাচাই করে।
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: বর্তমান প্রমাণ সংগ্রহ ও আবদ্ধ করে; কোনো মেরামত বা সময় নির্ধারণ নেই।
- `storage-sense-policy`: শুধু-পড়া স্বীকৃত কম্পিউটার পরিষ্কারের নীতি; ব্যবহারকারীর সেটিংস ও ফাইলের পরিচয় বাদ।
- `storage-sense-task`: শুধু-পড়া সঠিক টাস্কের অবস্থা ও রানটাইম ইতিহাস; কোনো টাস্ক চালানো বা পরিষ্কার নেই।
- `storage-service-events`: Storage Service, Optimize Drives, VSS ও সফটওয়্যার শ্যাডো প্রোভাইডারের সঠিক ব্যর্থতার ইতিহাস পড়ে।
- `volume-capacity DRIVE:`: JSON প্রমাণসহ শুধু-পড়া উপলব্ধ জায়গা।
- `volume-dirty DRIVE:`: শুধু-পড়া ডার্টি-বিটের অবস্থা; কোনো মেরামত বা রিস্টার্ট নেই।
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: ডিফল্টভাবে শুধু-পড়া বিশ্লেষণ; সঠিক সুস্থ ভলিউম ও সামঞ্জস্যপূর্ণ মিডিয়া দরকার।
- `volume-optimize-recover`: একটি মুলতুবি সম্পন্ন অপারেশন পুনরাবৃত্তি না করে যাচাই করে।
- `vss-providers`: শুধু-পড়া VSS প্রোভাইডারের নাম, সংস্করণ ও ধরন; শনাক্তকারী বাদ; কোনো নিবন্ধন পরিবর্তন নেই।
- `vss-writers`: জানানো রাইটারের অবস্থা; ইংরেজি আউটপুটের ব্যাখ্যা; কোনো রিসেট নেই।

## প্রিন্টিং

- `print-events`: শেষ সাত দিনের শুধু-পড়া PrintService Admin ইতিহাস; JSON প্রমাণ।
- `print-extension-files`: শুধু-পড়া প্রিন্ট প্রসেসর ও মনিটর বাইনারির উপস্থিতি ও স্বাক্ষরের সারাংশ; পূর্ণ পাথ বাদ।
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: ডিফল্টভাবে প্রিভিউ; কেবল সঠিক Error বা Blocked কাজ।
- `print-job-cancel-recover [--folder PATH]`: একটি বাধাপ্রাপ্ত বাতিলকরণ পুনরাবৃত্তি না করে আবার পরীক্ষা করে।
- `print-job-list`: বর্তমান দৃশ্যমান কাজগুলো পরিবর্তন না করে তালিকা করে।
- `print-operational-events`: শুধু-পড়া PrintService Operational চ্যানেলের অবস্থা ও উপলব্ধ ইতিহাস; JSON প্রমাণ।
- `print-policy`: শুধু-পড়া Point and Print সীমাবদ্ধতা ও ড্রাইভার-ইনস্টলেশন নীতি; JSON প্রমাণ।
- `print-rendering-policy`: শুধু-পড়া রেন্ডারিং ও ড্রাইভার-বিচ্ছিন্নতা নীতি; JSON প্রমাণ।
- `print-spooler-events`: স্থানীয় Print Spooler পরিচয়ের সঙ্গে যুক্ত শুধু-পড়া Service Control Manager ব্যর্থতা; JSON প্রমাণ।
- `printer-check --queue NAME [--queue NAME ...]`: শুধু-পড়া নির্বাচিত স্থানীয় কিউয়ের সেটিংস।
- `printer-conditions`: শুধু-পড়া স্থানীয় কিউয়ের অবস্থার কোড; কোনো ফিজিক্যাল যাচাই বা পরিবর্তন নেই।
- `printer-driver-files`: শুধু-পড়া নিবন্ধিত ফাইলের উপস্থিতি ও এক্সিকিউটেবল স্বাক্ষরের সারাংশ; পূর্ণ পাথ বাদ।
- `printer-driver-models`: শুধু-পড়া সংস্করণ 3, সংস্করণ 4 ও প্যাকেজ-সচেতনতার সারাংশ; JSON প্রমাণ।
- `printer-drivers`: শুধু-পড়া কিউ ও ড্রাইভার নিবন্ধনের তালিকা ও মিল; JSON প্রমাণ।
- `printer-endpoint-check`: সঠিক বরাদ্দ করা Standard TCP/IP প্রিন্টার এন্ডপয়েন্টে সীমিত সংযোগ; কোনো অনুসন্ধান, SNMP বা প্রিন্ট ডেটা নেই।
- `printer-list`: সেটিংস পরিবর্তন না করে স্থানীয় কিউ খুঁজে বের করে।
- `printer-ports`: শুধু-পড়া কিউ/পোর্ট নিবন্ধনের মিল; JSON প্রমাণ।
- `printer-queue-attributes`: শুধু-পড়া স্থানীয় কিউয়ের স্পুলিং, সংরক্ষণ ও শেয়ারিং বৈশিষ্ট্য।
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: ডিফল্টভাবে প্রিভিউ; অফলাইন মোড মুছে দিলে অপেক্ষমাণ কাজ মুক্ত হতে পারে।

## নেটওয়ার্ক

- `dhcp-check`: শুধু-পড়া অনুপস্থিত বা মেয়াদোত্তীর্ণ DHCP লিজের যোগ্যতা; JSON প্রমাণ, কোনো নবায়ন নেই।
- `dhcp-events`: শুধু-পড়া IPv4 ও IPv6 DHCP Client ঘটনার ইতিহাস; JSON ফলাফল।
- `dhcp-leases`: শুধু-পড়া লিজের টাইমস্ট্যাম্প, মেয়াদ ও সার্ভারের প্রমাণ; JSON ফলাফল।
- `dhcp-renew [--execute]`: ডিফল্টভাবে প্রিভিউ; কেবল সঠিক অনুপস্থিত বা মেয়াদোত্তীর্ণ সংযুক্ত ফিজিক্যাল DHCP লিজ।
- `dhcp-renew-recover`: আরেকটি লিজের অনুরোধ না করে শুধু-পড়া মিলিয়ে দেখা।
- `dns-cache HOST [--execute]`: ডিফল্টভাবে প্রিভিউ; সঠিক-হোস্ট যাচাইসহ সুরক্ষিত স্থানীয় ক্যাশ ফ্লাশ।
- `dns-cache-recover`: ফ্লাশ পুনরাবৃত্তি না করে একটি বাধাপ্রাপ্ত ক্যাশ অপারেশন আবার পরীক্ষা করে।
- `dns-cache-summary`: গোপনীয়তা-সীমিত স্থানীয় DNS ক্যাশের অবস্থা ও রেকর্ড-ধরনের সংখ্যা; কোনো পরিবর্তন নেই।
- `dns-check HOST [--timeout-seconds 1..60]`: সীমিত সিস্টেম নাম সমাধান; JSON ফলাফল।
- `dns-client-config`: শুধু-পড়া সাফিক্স, নিবন্ধন, ডেভোলিউশন ও নীতির প্রমাণ; JSON ফলাফল।
- `dns-client-events`: শুধু-পড়া গোপনীয়তা-সীমিত DNS Client সতর্কতা ও ব্যর্থতার ইতিহাস; JSON ফলাফল।
- `dns-nrpt-policy`: শুধু-পড়া কার্যকর DNS নেমস্পেস নীতির প্রমাণ; JSON ফলাফল।
- `ip-addresses`: শুধু-পড়া বর্তমান IP ঠিকানার অবস্থা, উৎস ও বিরোধ পর্যালোচনা; JSON প্রমাণ।
- `ip-interface-config`: শুধু-পড়া IP ইন্টারফেস কনফিগারেশনের প্রমাণ; JSON ফলাফল।
- `network-firewall`: সক্রিয় নেটওয়ার্ক বরাদ্দকে মিলে যাওয়া ফায়ারওয়াল প্রোফাইল চালু থাকার সঙ্গে মিলিয়ে দেখে।
- `network-offload`: শুধু-পড়া বৈশ্বিক Windows নেটওয়ার্ক অফলোড কনফিগারেশন; JSON ফলাফল।
- `network-profiles`: শুধু-পড়া বর্তমান প্রোফাইলের বিভাগ ও সংযোগের শ্রেণিবিভাগ; নেটওয়ার্কের নাম বাদ।
- `network-service-events`: DHCP, DNS, Workstation, NLA, Network List ও IP Helper-এর সঠিক ব্যর্থতার ইতিহাস পড়ে।
- `proxy-check`: শুধু-পড়া সংরক্ষিত WinHTTP ও বর্তমান অ্যাকাউন্টের ইন্টারনেট প্রক্সির প্রমাণ।
- `route-priority`: শুধু-পড়া কার্যকর ডিফল্ট-রুট মেট্রিকের তুলনা; JSON প্রমাণ।
- `smb-client-config`: শুধু-পড়া SMB সাইনিং, গেস্ট-অ্যাক্সেস ও ট্রান্সপোর্ট কনফিগারেশন।
- `smb-connections`: শুধু-পড়া সক্রিয় প্রোটোকল আলোচনা; রিমোট পরিচয় বাদ।
- `smb-server-config`: শুধু-পড়া SMB প্রোটোকল, সাইনিং, এনক্রিপশন ও মাল্টিচ্যানেল কনফিগারেশন।
- `smb-shares`: শুধু-পড়া সীমিত SMB শেয়ারের তালিকা; পাথ, অনুমতি, সেশন ও অ্যাকাউন্ট বাদ।
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: সীমিত সংযোগ পরীক্ষা; কোনো অ্যাপ্লিকেশন ডেটা পাঠানো হয় না।
- `tcp-port-pressure`: শুধু-পড়া ডায়নামিক TCP পরিসর ও সামগ্রিক স্থানীয়-পোর্ট ব্যবহার; এন্ডপয়েন্ট ও প্রসেস বাদ।
- `tcp-settings`: শুধু-পড়া Windows TCP টেমপ্লেট কনফিগারেশনের প্রমাণ; JSON ফলাফল।
- `vpn-profiles`: শুধু-পড়া বর্তমান অ্যাকাউন্টের VPN প্রোফাইল কনফিগারেশন; ক্রেডেনশিয়াল বাদ।
- `winrm-listeners`: শুধু-পড়া WinRM লিসেনার নিবন্ধনের সারাংশ; সার্টিফিকেটের থাম্বপ্রিন্ট বাদ।
- `winsock-catalog`: শুধু-পড়া নেটিভ Winsock প্রোটোকল ক্যাটালগের গঠন; JSON ফলাফল।
- `winsock-namespaces`: শুধু-পড়া নেটিভ Winsock নেমস্পেস-প্রোভাইডারের গঠন; JSON ফলাফল।
- `winsock-provider-files`: শুধু-পড়া প্রোটোকল প্রোভাইডার ফাইলের উপস্থিতি ও স্বাক্ষর; JSON ফলাফল।
- `winsock-readiness`: শুধু-পড়া নেটিভ Winsock প্রোটোকল, নেমস্পেস-প্রোভাইডার ও প্রোভাইডার-ফাইলের প্রস্তুতি; JSON ফলাফল।

## অ্যাপ

- `app-folder-review FOLDER_NAME [--report]`: নির্দিষ্ট-রুট সঠিক ফোল্ডার পর্যালোচনা; কোনো মুছে ফেলা নেই।
- `app-package-review FULL_NAME [--all-users]`: সঠিক লক্ষ্য ও নির্ভরশীল প্যাকেজ; কোনো অপসারণ নেই।
- `app-packages [--all-users]`: কেবল নিবন্ধিত Appx/MSIX তালিকা; কোনো অপসারণ নেই।
- `browser-extensions`: Chrome, Edge, Brave ও Firefox এক্সটেনশনের পরিচয় পর্যালোচনা; কোনো অপসারণ নেই।
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: কেবল পর্যালোচনা; কোনো সিস্টেম পরিবর্তন নেই।
- `depot-review [application and configuration selections] [--json]`: কেবল পর্যালোচনা; কোনো প্রভিশনিং নেই।
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: শুধু-পড়া Authenticode ও বিক্রেতা-স্বাক্ষরকারী পরীক্ষা; কিছুই চালানো হয় না।
- `foxit-inventory`: শুধু-পড়া কম্পিউটারের পণ্যের তালিকা; JSON আউটপুট।
- `installed-apps [--current-user]`: আনইনস্টল নিবন্ধন ও সক্ষমতার ফ্ল্যাগ; কোনো অপসারণ নেই।
- `installed-apps-compare BEFORE.json AFTER.json`: সংরক্ষিত তালিকার নিবন্ধনের পার্থক্য; কোনো কম্পিউটার পরিবর্তন নেই।
- `installed-hotfixes`: শুধু-পড়া সীমিত ইনস্টল করা Windows হটফিক্সের তালিকা; JSON প্রমাণ।
- `installer-activity`: শুধু-পড়া Installer প্রসেস ও লেনদেন-চিহ্নের প্রমাণ; JSON প্রমাণ।
- `installer-check`: শুধু-পড়া Installer নীতি ও ইঞ্জিনের প্রস্তুতি; JSON প্রমাণ।
- `installer-engine-files`: শুধু-পড়া প্রয়োজনীয় Installer ফাইলের উপস্থিতি ও Authenticode সারাংশ; JSON প্রমাণ।
- `installer-events`: শেষ সাত দিনের শুধু-পড়া MSI ইতিহাস; JSON প্রমাণ।
- `installer-logging`: শুধু-পড়া Installer লগিং ও ডিবাগ নীতি; JSON প্রমাণ।
- `installer-pending-state`: শুধু-পড়া মুলতুবি ফাইল অপারেশন ও Installer কার্যক্রমের মিল; JSON প্রমাণ।
- `installer-registration-repair [--execute]`: ডিফল্টভাবে প্রিভিউ; কেবল সঠিক সার্ভিস-নিবন্ধনের ত্রুটি; কখনো ইঞ্জিনের নিবন্ধন বাতিল করে না।
- `installer-registration-repair-recover`: কমান্ড পুনরাবৃত্তি না করে বাধাপ্রাপ্ত নিবন্ধন আবার পরীক্ষা করে।
- `installer-security-policy`: শুধু-পড়া Installer নিরাপত্তা ও প্যাচ নীতি; JSON প্রমাণ।
- `installer-service-events`: স্থানীয় Windows Installer পরিচয়ের সঙ্গে যুক্ত শুধু-পড়া Service Control Manager ব্যর্থতা; JSON প্রমাণ।
- `installer-service-registration`: শুধু-পড়া Windows Installer সার্ভিস নিবন্ধনের সারাংশ; JSON প্রমাণ।
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: নতুন সঠিক যোগ্যতার প্রিভিউ; কোনো অপসারণ প্রোভাইডার নেই।
- `mace-package-recover`: একটি বাধাপ্রাপ্ত অ্যাপ প্যাকেজ-অপসারণ রেকর্ড নতুনভাবে মিলিয়ে দেখে; কখনো অপসারণ পুনরাবৃত্তি করে না।
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: কেবল পর্যালোচনা; কোনো অপসারণ নেই।
- `msi-cache`: শুধু-পড়া নিবন্ধিত Windows Installer ক্যাশের দৃশ্যমানতা; JSON প্রমাণ।
- `msi-code NONNEGATIVE_DECIMAL`: নথিভুক্ত Windows Installer রিটার্ন-কোড রেফারেন্স; কোনো কম্পিউটার পরিদর্শন বা মেরামত নেই।
- `msi-product {PRODUCT-CODE}`: শুধু-পড়া সঠিক MSI নিবন্ধনের অবস্থা; JSON প্রমাণ।
- `msi-repair {PRODUCT-CODE} [--execute]`: ডিফল্টভাবে প্রিভিউ; কেবল সঠিক ভাঙা বা ভুল-কনফিগারেশনের পণ্য।
- `msi-repair-recover`: একটি বাধাপ্রাপ্ত সঠিক-পণ্য মেরামত পুনরাবৃত্তি না করে আবার পরীক্ষা করে।
- `msi-sources`: শুধু-পড়া গোপনীয়তা-সীমিত নিবন্ধিত Windows Installer উৎসের মেটাডেটা; JSON প্রমাণ।
- `orca-cache-reset --mode New|Classic|Both [--execute]`: ডিফল্টভাবে প্রিভিউ; চলমান অ্যাকাউন্টের সঠিক ক্যাশ পুনরুদ্ধার নামে রেখে দেয়।
- `orca-cache-reset-recover`: স্থানান্তর পুনরাবৃত্তি না করে বাধাপ্রাপ্ত ক্যাশ সংরক্ষণ যাচাই করে।
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: কেবল পর্যালোচনা; কোনো Outlook পরিবর্তন নেই।
- `startup-apps`: স্টার্টআপ নিবন্ধন এবং বুট বা লগঅন টাস্কের পর্যালোচনা; কোনো নিষ্ক্রিয়করণ নেই।
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: ডিফল্টভাবে প্রিভিউ; কেবল একটি সঠিক সংস্করণ।
- `third-party-update-recover`: আপডেট পুনঃচেষ্টা না করে একটি মুলতুবি সঠিক সংস্করণ যাচাই করে।
- `third-party-updates`: শুধু-পড়া Microsoft.WinGet.Client অ্যাপ্লিকেশন আপডেট অনুসন্ধান; কোনো অ্যাপ্লিকেশন পরিবর্তন নেই।

## সিস্টেম

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: সঠিক স্থানীয় Bluetooth SIG বরাদ্দের অনুসন্ধান; কোনো নেটওয়ার্ক অনুরোধ নেই।
- `crash-config`: সংরক্ষিত Windows ডাম্প মোড ও পাথ; কোনো পরিবর্তন নেই।
- `crash-dumps`: ডিফল্ট Windows ডাম্প-ফাইলের মেটাডেটা; কোনো বিষয়বস্তু বিশ্লেষণ বা পরিবর্তন নেই।
- `crash-events`: সাম্প্রতিক সিস্টেম ত্রুটি রিপোর্ট; কোনো ডায়াগনসিস বা পরিবর্তন নেই।
- `crash-review [--knowledge PACK_PATH ...]`: সর্বোচ্চ 32টি প্যাক; ক্র্যাশ, শাটডাউন, সীমিত মিনিডাম্প কাঠামো ও ঐচ্ছিক সঠিক বাগ-চেক রেফারেন্সের সম্মিলিত পর্যালোচনা; কোনো পরিবর্তন নেই।
- `default-printer`: শুধু-পড়া ডিফল্ট নির্বাচন ও স্বয়ংক্রিয়-ব্যবস্থাপনার সেটিং; JSON প্রমাণ।
- `default-printer-set QUEUE [--execute] [--folder PATH]`: ডিফল্টভাবে প্রিভিউ; কেবল অনুপস্থিত বা বিরোধযুক্ত ডিফল্ট।
- `default-printer-set-recover [--folder PATH]`: একটি বাধাপ্রাপ্ত নির্বাচন পুনরাবৃত্তি না করে আবার পরীক্ষা করে।
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: সর্বোচ্চ 32টি প্যাক থেকে ঐচ্ছিক সঠিক ড্রাইভার রেফারেন্সসহ শুধু-পড়া ডিভাইসের প্রমাণ।
- `device-code DECIMAL_OR_0xHEX`: অফলাইন Device Manager সমস্যা-কোড রেফারেন্স।
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: কেবল রেফারেন্সের প্রার্থী।
- `event-channel-enable CHANNEL [--execute]`: ডিফল্টভাবে প্রিভিউ; একটি সঠিক অব্যবস্থাপিত ডায়াগনস্টিক চ্যানেল চালু করে এবং অন্য সেটিংস সংরক্ষণ করে।
- `event-channel-enable-recover`: চালু করার কমান্ড পুনরাবৃত্তি না করে শুধু-পড়া মিলিয়ে দেখা।
- `event-channels`: চারটি ডায়াগনস্টিক চ্যানেলের শুধু-পড়া অবস্থা, সংরক্ষণ, আকার ও পূর্ণতা।
- `font-registrations`: শুধু-পড়া সীমিত কম্পিউটার ও বর্তমান-ব্যবহারকারীর নিবন্ধন এবং ফাইলের উপস্থিতি পরীক্ষা।
- `memory-diagnostic-results`: শুধু-পড়া সীমিত Windows মেমরি ডায়াগনস্টিকের ইতিহাস; JSON প্রমাণ।
- `minidump-analyze PATH`: সীমিত কাঠামোগত মিনিডাম্প মেটাডেটা; কোনো মেমরি পেজ, সিম্বল বা পরিবর্তন নেই।
- `name-rename PROPOSED_NAME [--execute]`: ডিফল্টভাবে প্রিভিউ; কেবল স্বতন্ত্র ওয়ার্কগ্রুপ পিসি; কখনো Windows রিস্টার্ট করে না।
- `name-rename-recover`: একটি মুলতুবি নাম পরিবর্তন পুনরাবৃত্তি না করে যাচাই করে।
- `name-review PROPOSED_NAME`: শুধু-পড়া পরিচয় ও প্রস্তাবিত নামের পর্যালোচনা।
- `profiles --recover`: রেকর্ড করা অনুপস্থিতি যাচাই করে এবং পুনরুদ্ধারের প্রমাণ রাখে; কখনো মুছে ফেলার পুনঃচেষ্টা করে না।
- `profiles --recover-backup`: একটি মুলতুবি ব্যাকআপ আর্টিফ্যাক্ট যাচাই ও আর্কাইভ করে; কখনো কপির পুনঃচেষ্টা করে না।
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: প্রথমে প্রিভিউ; সুরক্ষিত নেটিভ কপির জন্য স্পষ্ট এক্সিকিউশন এবং একটি অনুপস্থিত স্থানীয় NTFS গন্তব্য দরকার।
- `profiles --select SID --create-backup-artifact DESTINATION`: একটি বিদ্যমান কপি করা ব্যাকআপ তুলনা করে এবং এর নির্দিষ্ট আর্টিফ্যাক্ট তৈরি করে; কোনো প্রোফাইল কপি বা অপসারণ করে না।
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: শুধু-পড়া পর্যালোচনা; যাচাইয়ের জন্য একটি নির্বাচিত SID দরকার।
- `task-check`: শুধু-পড়া টাস্কের তালিকা, শ্রেণিবদ্ধ ফলাফল ও JSON প্রমাণ।
- `task-definition TASK_FOLDER TASK_NAME`: শুধু-পড়া সঠিক টাস্ক XML; অনুপলব্ধ মানে অনুপস্থিত নয়।
- `task-inventory`: শুধু-পড়া টাস্কের পরিচয় ও অবস্থা; JSON আউটপুট।
- `task-journal RECORD.json [--completion COMPLETION.json]`: ঐতিহাসিক পুনরুদ্ধার পর্যালোচনা; কখনো পুনঃচেষ্টার অনুমতি দেয় না।
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: একটি টাস্কের খসড়া যাচাই ও পর্যালোচনা করে; কোনো নিবন্ধন নেই।
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: ডিফল্টভাবে প্রিভিউ; নিষ্ক্রিয়, কেবল-তৈরি Local System সংজ্ঞা।
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: শুধু-পড়া সঠিক-সংজ্ঞা মিলিয়ে দেখা; কখনো কোনো টাস্কের পুনঃচেষ্টা করে না বা চালায় না।
- `task-xml PLAN.json YYYY-MM-DD`: stdout-এ নিষ্ক্রিয় পর্যালোচনা XML; কেবল স্থানীয়-অঞ্চলের ক্যালেন্ডার সময়সূচি; কোনো নিবন্ধন নেই।
- `time-config`: শুধু-পড়া Windows Time মোড, পিয়ার ও নীতির প্রমাণ।
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: ডিফল্টভাবে প্রিভিউ; কাস্টম মোডের জন্য পিয়ার দরকার।
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: কেবল প্রিভিউ।
- `time-recover --folder PATH`: একটি মুলতুবি রেকর্ড যাচাই ও আর্কাইভ করে; কখনো পরিবর্তনের পুনঃচেষ্টা করে না।
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: শুধু-পড়া; কাস্টম মোডের জন্য পিয়ার দরকার।
- `wmi-provider`: শুধু-পড়া স্থানীয় অপারেটিং-সিস্টেম প্রোভাইডার কোয়েরি; JSON প্রমাণ।

## নলেজ, যাচাই ও Engine

- `knowledge PACK_PATH CONTEXT CODE`: স্থানীয় রেফারেন্স অনুসন্ধান; কোনো মেরামত চালানো নেই।
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: যাচাইকৃত স্থানীয় সক্রিয়করণ; কোনো ডাউনলোড নেই।
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: সক্রিয় স্থানীয় প্যাক যাচাই করে; কোনো পরিবর্তন নেই।
- `knowledge-compare BEFORE_JSON AFTER_JSON`: কেবল রেফারেন্সের পরিবর্তন; কোনো প্যাক ইনস্টলেশন নেই।
- `knowledge-info PACK_PATH`: প্যাকের মেটাডেটা ও প্রতি-প্রেক্ষাপট এন্ট্রির সংখ্যা; কোনো পরিবর্তন নেই।
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: পর্যবেক্ষিত কোডের সঙ্গে প্রাসঙ্গিক রেফারেন্স যুক্ত করে।
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: সর্বশেষ যাচাইকৃত সংরক্ষিত প্যাক নির্বাচন করে; কোনো ডাউনলোড নেই।
- `knowledge-search PACK_PATH CONTEXT TEXT`: সর্বোচ্চ 50টি রেফারেন্স টেক্সটের মিল; কোনো মেরামত চালানো নেই।
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: সর্বোচ্চ 32টি প্যাক জুড়ে একটি সঠিক পরিচয় সমাধান করে; বিরোধ কখনো নির্বাচিত হয় না।
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: প্রতিটি কনফিগার করা ইনস্টল করা ডেটাবেস যাচাই করে; কোনো ডাউনলোড বা সক্রিয়করণ নেই।
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: কনফিগার করা HTTPS এন্ডপয়েন্ট থেকে স্বাক্ষরিত ডেটাবেস প্রার্থী এনে অ্যাটমিকভাবে সক্রিয় করে।
- `open <file.kbench> [--json]`: একটি সংরক্ষিত ক্যাপচার আবার খোলে এবং ইনস্টল করা নিয়ম দিয়ে আবার মূল্যায়ন করে।
- `scan --output <new-file.kbench> [--json]`: একটি নতুন ফাইলে সংরক্ষিত শুধু-পড়া উইন্ডোজ আপডেট ক্যাপচার।
- `validation-coverage RECORD.json [...]`: বর্তমান সংস্করণের নেটিভ VM প্রমাণের সারাংশ; কোনো ডায়াগনস্টিক্স বা মেরামত নেই।
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: শুধু-পড়া নিষ্পত্তিযোগ্য-VM দৃশ্যপট পরিকল্পনা; কোনো ডায়াগনস্টিক্স বা মেরামত নেই।
- `validation-record RECORD.json`: একটি সীমিত নেটিভ VM প্রমাণ রেকর্ড যাচাই করে; কোনো ডায়াগনস্টিক্স বা মেরামত নেই।
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: স্থানীয় বর্তমান-সংস্করণ VM কভারেজ রিপোর্ট।
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: একটি যাচাইকৃত শুধু-পড়া শেয়ার করা বিকল্প পরিকল্পনা চালায়।
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: কেবল তালিকা।
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: নির্দিষ্ট ডায়াগনস্টিক জার্নাল; কোনো মেরামত নেই।
- `workflow computer-name --name PROPOSED_NAME [--json]`: কেবল জার্নালভুক্ত পর্যালোচনা।
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: জার্নালে অন্তর্ভুক্ত ঐচ্ছিক সঠিক পণ্যের অবস্থা।
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: জার্নালে অন্তর্ভুক্ত ঐচ্ছিক নির্বাচিত-লক্ষ্যের ডায়াগনসিস।
- `workflow storage [--volume D:] [--json]`: জার্নালভুক্ত শুধু-পড়া ফাইল সিস্টেম পরীক্ষা।
- `workflow-journal FILE.json`: ঐতিহাসিক বিষয়বস্তু; এক্সিট 0 মানে সফলভাবে পড়া হয়েছে, সুস্থ নয়।
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: হোস্ট-নিরপেক্ষ নিয়ন্ত্রণ দেখায় বা যাচাই করে; কোনো স্ক্যান বা মেরামত নেই।
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: কেবল ঐতিহাসিক রিপোর্ট; কোনো কমান্ড চালানো হয় না।

## অন্যান্য কমান্ড

- `<analyze|investigate> <text-log-path> [--json]`: স্থানীয় টেক্সট প্রমাণ বিশ্লেষণ করে।
- `adapter-bindings`: শুধু-পড়া মূল প্রোটোকল বাইন্ডিংয়ের সারাংশ; JSON প্রমাণ।
- `adapter-statistics`: শুধু-পড়া অ্যাডাপ্টারের বাইট, ত্রুটি ও বাতিলের কাউন্টার; JSON প্রমাণ।
- `application-events`: শুধু-পড়া সাম্প্রতিক Application ত্রুটি, গোষ্ঠীবদ্ধ সংখ্যা ও JSON প্রমাণ।
- `applocker-policy`: শুধু-পড়া কার্যকর সংগ্রহ মোড ও সামগ্রিক নিয়মের সংখ্যা; নিয়মের বিবরণ বাদ; কোনো পরিবর্তন নেই।
- `common-check [--scope NAME ...]`: ডিফল্টভাবে সব ডায়াগনস্টিক ক্ষেত্র; নির্দিষ্ট করা হলে কেবল নির্বাচিত ক্ষেত্র; শুধু-পড়া JSON।
- `delete-notifications`: শুধু-পড়া NTFS ও ReFS ডিলিট-নোটিফিকেশন কনফিগারেশন; JSON; কোনো অপটিমাইজেশন বা সেটিং পরিবর্তন নেই।
- `diagnostic-scopes [--all]`: 11টি প্রস্তাবিত ফলাফল গোষ্ঠী, বা সব 49টি শুধু-পড়া ক্ষেত্রের তালিকা করে; কোনো সংগ্রহ নেই।
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: নির্দিষ্ট ডায়াগনস্টিক জার্নাল; কোনো মেরামত নেই।
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: সব ডায়াগনস্টিক ক্ষেত্র; `--auto-repair` স্পষ্ট না হলে কোনো মেরামত নেই।
- `gateway-check`: সঠিক কনফিগার করা ডিফল্ট পরবর্তী হপের সীমিত ICMP পরীক্ষা; কোনো ঠিকানা স্ক্যান নেই।
- `infrastructure-service-events`: একটি সীমিত সঠিক সংযোগ, রিমোট-ব্যবস্থাপনা, বিজ্ঞপ্তি ও মূল সার্ভিস ব্যর্থতার স্ন্যাপশট পড়ে।
- `investigate-batch FILE [FILE ...]`: সর্বোচ্চ 12টি ফাইল; JSON Lines; ব্যর্থ বা আংশিক আমদানির জন্য এক্সিট 3।
- `licensing-check`: প্রোডাক্ট কী ছাড়া শুধু-পড়া Windows অ্যাক্টিভেশনের অবস্থা; JSON প্রমাণ।
- `mac-vendor PACK_PATH MAC_ADDRESS`: দীর্ঘতম-প্রিফিক্স স্থানীয় প্যাক অনুসন্ধান; কোনো নেটওয়ার্ক অনুরোধ নেই।
- `ncsi-policy`: শুধু-পড়া Windows সংযোগ শ্রেণিবিভাগ নীতি; প্রোবের গন্তব্য বাদ।
- `neighbor-cache`: শুধু-পড়া গোপনীয়তা-সীমিত প্রতিবেশীর পৌঁছানোর প্রমাণ; JSON ফলাফল।
- `ntfs-self-healing DRIVE:`: শুধু-পড়া নির্বাচিত ভলিউমের অনলাইন NTFS মেরামত কনফিগারেশন; কোনো মেরামত বা সেটিং পরিবর্তন নেই।
- `pagefile-check`: শুধু-পড়া পেজ-ফাইল ব্যবস্থাপনা, বরাদ্দ ও ব্যবহারের সারাংশ; পূর্ণ পাথ বাদ।
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: সঠিক স্থানীয় প্যাক অনুসন্ধান; কোনো নেটওয়ার্ক অনুরোধ বা ড্রাইভার নির্বাচন নেই।
- `physical-disks`: শুধু-পড়া বর্তমান স্বাস্থ্য, কার্যক্রম, মিডিয়া, বাস ও ব্যবহারের অবস্থা; JSON; কোনো মেরামত নেই।
- `platform-service-events`: একটি সীমিত সঠিক অ্যাপ্লিকেশন, পরিচয়, প্রোফাইল ও ডিভাইস-সহায়তা সার্ভিস ব্যর্থতার স্ন্যাপশট পড়ে।
- `previous-windows [--execute --backup-confirmed]`: ডিফল্টভাবে প্রিভিউ; DISM-এর মাধ্যমে Windows রোলব্যাক সক্ষমতা স্থায়ীভাবে সরিয়ে দেয়।
- `previous-windows-recover`: রোলব্যাক অপসারণ পুনরাবৃত্তি না করে শুধু-পড়া মিলিয়ে দেখা।
- `provisioned-packages`: স্থানীয় Windows-এ নতুন ব্যবহারকারীদের জন্য প্রস্তুত প্যাকেজ; কোনো অপসারণ নেই।
- `recipes [WORKFLOW]`: ইনস্টল করা সংজ্ঞা বা ওয়ার্কফ্লো-নির্দিষ্ট নির্ভরতা ও সীমার তালিকা করে; কোনো এক্সিকিউশন নেই।
- `repair <dism|sfc> [--execute]`: ডিফল্টভাবে প্রিভিউ।
- `reserved-storage`: শুধু-পড়া অনলাইন Windows সংরক্ষিত-স্টোরেজের অবস্থা; JSON প্রমাণ।
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: ডিফল্টভাবে প্রিভিউ; নতুন পরীক্ষার পরে একটি পয়েন্ট তৈরি করে।
- `restore-point-recover`: আরেকটি পয়েন্ট তৈরি না করে একটি মুলতুবি অনুরোধ যাচাই করে।
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: কেবল প্রিভিউ।
- `shadow-copies`: শুধু-পড়া বর্তমান VSS স্ন্যাপশটের অবস্থা ও বৈশিষ্ট্য; পরিচয় বাদ; কোনো স্ন্যাপশট পরিবর্তন নেই।
- `shadow-storage`: শুধু-পড়া VSS ডিফারেনশিয়াল-এরিয়ার বরাদ্দ ও চাপ; JSON; কোনো আকার পরিবর্তন বা স্ন্যাপশটের কাজ নেই।
- `uac-policy`: শুধু-পড়া UAC চালু থাকা ও এলিভেশন-প্রম্পট নীতি; কোনো অ্যাকাউন্ট বা ক্রেডেনশিয়াল ডেটা নেই।
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: সঠিক স্থানীয় UEFI রেজিস্ট্রি অনুসন্ধান; কোনো নেটওয়ার্ক অনুরোধ বা ড্রাইভার নির্বাচন নেই।
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: সঠিক স্থানীয় প্যাক অনুসন্ধান; কোনো নেটওয়ার্ক অনুরোধ বা ড্রাইভার নির্বাচন নেই।
- `vitals-check`: শুধু-পড়া Windows, হার্ডওয়্যার, অ্যাকাউন্ট, রিসোর্স, ব্যাটারি ও ফিজিক্যাল-ডিস্কের স্ন্যাপশট; JSON প্রমাণ।
- `win32-code DECIMAL_OR_0xHEX`: অফলাইন ইনস্টল করা Windows বার্তা অনুসন্ধান।
- `workflows`: ইনস্টল করা ওয়ার্কফ্লো কী ও শিরোনামের তালিকা করে; কোনো স্ক্যান বা মেরামত নেই।
