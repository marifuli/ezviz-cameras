<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="csrf-token" content="{{ csrf_token() }}">
    <title>@yield('title', 'Dashboard') - Ezviz Camera</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <!-- DataTables CSS -->
    <link href="https://cdn.datatables.net/1.13.7/css/jquery.dataTables.min.css" rel="stylesheet">
    <!-- Select2 CSS -->
    <link href="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css" rel="stylesheet">

    <!-- jQuery (required for DataTables and Select2) -->
    <script src="https://code.jquery.com/jquery-3.7.1.min.js"></script>
    <!-- DataTables JS -->
    <script src="https://cdn.datatables.net/1.13.7/js/jquery.dataTables.min.js"></script>
    <!-- Select2 JS -->
    <script src="https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js"></script>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css" integrity="sha512-DTOQO9RWCH3ppGqcWaEA1BIZOC6xxalwEsw9c2QQeAIftl+Vegovlnee1c9QX4TctnWMn13TZye+giMm8e2LwA==" crossorigin="anonymous" referrerpolicy="no-referrer" />

    <script src="https://cdn.jsdelivr.net/npm/toastr@2.1.4/toastr.min.js"></script>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/toastr@2.1.4/build/toastr.min.css">

    @stack('styles')
</head>
<body class="bg-gray-50 text-gray-900">
    <nav class="bg-white text-gray-900 shadow-lg border-b border-gray-200">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div class="flex justify-between h-16">
                <div class="flex items-center">
                    <a href="{{ route('dashboard') }}" class="text-xl font-bold hover:text-gray-700">Ezviz Camera</a>
                </div>

                <div class="hidden md:flex items-center space-x-8">
                    <a href="{{ route('dashboard') }}" class="hover:text-gray-700 transition duration-150 flex items-center">
                        <svg version="1.0" class="w-5 h-5 mr-1" id="Layer_1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink"
                            width="50px" height="50px" viewBox="0 0 64 64" enable-background="new 0 0 64 64" xml:space="preserve">
                            <g>
                                <circle fill="#F9EBB2" cx="32" cy="32" r="30"/>
                                <g>
                                    <path fill="#394240" d="M32,0C14.355,0,0,14.355,0,32s14.355,32,32,32s32-14.355,32-32S49.645,0,32,0z M32,62
                                        C15.458,62,2,48.542,2,32S15.458,2,32,2s30,13.458,30,30S48.542,62,32,62z"/>
                                    <path fill="#394240" d="M34.996,28.021L35,28.008V17c0-1.654-1.346-3-3-3s-3,1.346-3,3v11l0.004,0.021
                                        C27.795,28.936,27,30.371,27,32c0,2.757,2.243,5,5,5s5-2.243,5-5C37,30.371,36.205,28.936,34.996,28.021z M31,17
                                        c0-0.552,0.448-1,1-1s1,0.448,1,1v10.102C32.677,27.035,32.343,27,32,27s-0.677,0.035-1,0.102V17z M32,35c-1.654,0-3-1.346-3-3
                                        s1.346-3,3-3s3,1.346,3,3S33.654,35,32,35z"/>
                                    <path fill="#394240" d="M32.03,31H32.02c-0.552,0-0.994,0.447-0.994,1s0.452,1,1.005,1c0.552,0,1-0.447,1-1S32.582,31,32.03,31z"/>
                                </g>
                                <g>
                                    <path fill="#B4CCB9" d="M32,29c-1.654,0-3,1.346-3,3s1.346,3,3,3s3-1.346,3-3S33.654,29,32,29z M32.03,33
                                        c-0.553,0-1.005-0.447-1.005-1s0.442-1,0.994-1h0.011c0.552,0,1,0.447,1,1S32.582,33,32.03,33z"/>
                                </g>
                                <path fill="#F76D57" d="M33,27.102V17c0-0.552-0.448-1-1-1s-1,0.448-1,1v10.102C31.323,27.035,31.657,27,32,27
                                    S32.677,27.035,33,27.102z"/>
                            </g>
                        </svg>
                        Dashboard
                    </a>
                    <a href="{{ route('footage.index') }}" class="hover:text-gray-700 transition duration-150 flex items-center">
                        <svg class="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                        </svg>
                        Footages
                    </a>
                    <a href="{{ route('jobs.index') }}" class="hover:text-gray-700 transition duration-150 flex items-center">
                        <svg class="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
                        </svg>
                        Jobs
                    </a>
                    <a href="{{ route('cameras.index') }}" class="hover:text-gray-700 transition duration-150 flex items-center">
                        <svg class="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                        </svg>
                        Cameras
                    </a>
                </div>

                <!-- Desktop Account Dropdown -->
                <div class="hidden md:flex items-center">
                    <div class="relative">
                        <button id="accountDropdown" class="flex items-center hover:text-gray-700 transition duration-150 focus:outline-none">
                            <svg class="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"></path>
                            </svg>
                            Account
                            <svg class="ml-1 h-4 w-4" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd"></path>
                            </svg>
                        </button>
                        <div id="accountMenu" class="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg z-10 hidden border border-gray-200">
                            <a href="{{ route('change-password') }}" class="flex items-center px-4 py-2 text-sm text-gray-700 hover:bg-gray-100">
                                <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 712 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1721 9z"></path>
                                </svg>
                                Change Password
                            </a>
                            <form method="POST" action="{{ route('logout') }}" class="inline">
                                @csrf
                                <button type="submit" class="flex items-center w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100">
                                    <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
                                    </svg>
                                    Logout
                                </button>
                            </form>
                        </div>
                    </div>
                </div>

                <!-- Mobile menu button -->
                <div class="md:hidden flex items-center">
                    <button id="mobileMenuButton" class="text-gray-900 hover:text-gray-700 focus:outline-none focus:text-gray-700 transition duration-150">
                        <svg id="mobileMenuIcon" class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"></path>
                        </svg>
                        <svg id="mobileCloseIcon" class="w-6 h-6 hidden" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
                        </svg>
                    </button>
                </div>
            </div>

            <!-- Mobile Navigation Menu -->
            <div id="mobileMenu" class="md:hidden hidden">
                <div class="px-2 pt-2 pb-3 space-y-1 bg-white border-t border-gray-200">
                    <a href="{{ route('dashboard') }}" class="block px-3 py-2 text-base font-medium text-gray-900 hover:text-gray-700 hover:bg-gray-50 transition duration-150 rounded-md">
                        <span class="flex items-center">
                            <i class="fas fa-tachometer-alt w-5 h-5 mr-2"></i>
                            Dashboard
                        </span>
                    </a>
                    <a href="{{ route('footage.index') }}" class="block px-3 py-2 text-base font-medium text-gray-900 hover:text-gray-700 hover:bg-gray-50 transition duration-150 rounded-md">
                        <span class="flex items-center">
                            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                            </svg>
                            Footage Archive
                        </span>
                    </a>
                    <a href="{{ route('jobs.index') }}" class="block px-3 py-2 text-base font-medium text-gray-900 hover:text-gray-700 hover:bg-gray-50 transition duration-150 rounded-md">
                        <span class="flex items-center">
                            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
                            </svg>
                            Download Jobs
                        </span>
                    </a>
                    <a href="{{ route('cameras.index') }}" class="block px-3 py-2 text-base font-medium text-gray-900 hover:text-gray-700 hover:bg-gray-50 transition duration-150 rounded-md">
                        <span class="flex items-center">
                            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"></path>
                            </svg>
                            Cameras
                        </span>
                    </a>
                </div>
            </div>
        </div>
    </nav>

    <main class="min-h-screen py-8">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            @yield('content')
        </div>
    </main>

    <script>
        // Account dropdown toggle
        document.getElementById('accountDropdown')?.addEventListener('click', function() {
            document.getElementById('accountMenu')?.classList.toggle('hidden');
        });

        // Mobile menu toggle
        document.getElementById('mobileMenuButton')?.addEventListener('click', function() {
            const mobileMenu = document.getElementById('mobileMenu');
            const mobileMenuIcon = document.getElementById('mobileMenuIcon');
            const mobileCloseIcon = document.getElementById('mobileCloseIcon');

            if (mobileMenu) {
                mobileMenu.classList.toggle('hidden');
                mobileMenuIcon?.classList.toggle('hidden');
                mobileCloseIcon?.classList.toggle('hidden');
            }
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', function(event) {
            const dropdown = document.getElementById('accountMenu');
            const button = document.getElementById('accountDropdown');
            if (dropdown && button && !button.contains(event.target) && !dropdown.contains(event.target)) {
                dropdown.classList.add('hidden');
            }
        });

        // Close mobile menu when clicking outside or on mobile links
        document.addEventListener('click', function(event) {
            const mobileMenu = document.getElementById('mobileMenu');
            const mobileMenuButton = document.getElementById('mobileMenuButton');
            const mobileMenuIcon = document.getElementById('mobileMenuIcon');
            const mobileCloseIcon = document.getElementById('mobileCloseIcon');

            // Close if clicking outside mobile menu area
            if (mobileMenu && mobileMenuButton &&
                !mobileMenu.contains(event.target) &&
                !mobileMenuButton.contains(event.target) &&
                !mobileMenu.classList.contains('hidden')) {
                mobileMenu.classList.add('hidden');
                mobileMenuIcon?.classList.remove('hidden');
                mobileCloseIcon?.classList.add('hidden');
            }
        });

        // Close mobile menu when clicking on mobile menu links
        document.querySelectorAll('#mobileMenu a').forEach(function(link) {
            link.addEventListener('click', function() {
                const mobileMenu = document.getElementById('mobileMenu');
                const mobileMenuIcon = document.getElementById('mobileMenuIcon');
                const mobileCloseIcon = document.getElementById('mobileCloseIcon');

                setTimeout(function() {
                    mobileMenu?.classList.add('hidden');
                    mobileMenuIcon?.classList.remove('hidden');
                    mobileCloseIcon?.classList.add('hidden');
                }, 100);
            });
        });

        // Setup CSRF token for AJAX requests
        $.ajaxSetup({
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            }
        });
    </script>

    @stack('scripts')
</body>
</html>
