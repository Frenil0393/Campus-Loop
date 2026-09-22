/**
 * CampusLoop Marketplace Interactive JavaScript
 * Implements R.2 (Marketplace Browsing), R.4 (Search & Categories),
 * R.6 (Real-Time Chat Interaction), R.14 (3-4 Image Gallery), and R.3 (Listing Lifecycle)
 */

document.addEventListener('DOMContentLoaded', () => {
    // State management
    const state = {
        category: 'all',
        keyword: '',
        minPrice: 0,
        maxPrice: 99999,
        sort: 'newest',
        wishlist: JSON.parse(localStorage.getItem('campusloop_wishlist') || '[]'),
        activeProductChat: null
    };

    // DOM Elements
    const productGrid = document.getElementById('productsGrid');
    const productCards = document.querySelectorAll('.product-card-col');
    const liveSearchInput = document.getElementById('liveSearchInput');
    const clearSearchBtn = document.getElementById('clearSearchInputBtn');
    const heroSearchInput = document.getElementById('heroSearchInput');
    const heroSearchBtn = document.getElementById('heroSearchBtn');
    const heroTags = document.querySelectorAll('.hero-tag');
    const categoryPills = document.querySelectorAll('.category-pill-btn');
    const budgetButtons = document.querySelectorAll('#budgetFiltersGroup button');
    const sortDropdown = document.getElementById('sortDropdown');
    const visibleCountEl = document.getElementById('visibleProductCount');
    const noProductsFound = document.getElementById('noProductsFound');
    const resetEmptyStateBtn = document.getElementById('resetEmptyStateBtn');
    const clearAllFiltersBtn = document.getElementById('clearAllFiltersBtn');
    const wishlistBadge = document.getElementById('wishlistCountBadge');

    // Initialize Wishlist State
    updateWishlistUI();

    // -------------------------------------------------------------
    // 1. FILTERING & SEARCH LOGIC (R.4.1 & R.4.2)
    // -------------------------------------------------------------
    function applyFilters() {
        let visibleCount = 0;
        const term = state.keyword.trim().toLowerCase();

        productCards.forEach(col => {
            const name = col.getAttribute('data-name') || '';
            const category = col.getAttribute('data-category') || '';
            const price = parseFloat(col.getAttribute('data-price') || '0');
            const status = col.getAttribute('data-status') || '';

            // Exclude sold items from available marketplace (R.2.1)
            const isAvailable = (status === 'AVAILABLE');

            // Category match
            const matchesCategory = (state.category === 'all' || category === state.category);

            // Search keyword match
            const matchesKeyword = !term || name.includes(term) || category.includes(term);

            // Price range match
            const matchesPrice = (price >= state.minPrice && price <= state.maxPrice);

            if (isAvailable && matchesCategory && matchesKeyword && matchesPrice) {
                col.style.display = '';
                visibleCount++;
            } else {
                col.style.display = 'none';
            }
        });

        // Update counts and empty state
        if (visibleCountEl) visibleCountEl.textContent = visibleCount;
        if (noProductsFound) {
            noProductsFound.style.display = visibleCount === 0 ? 'block' : 'none';
        }

        // Show/hide clear all button
        const isFiltered = state.category !== 'all' || state.keyword !== '' || state.minPrice > 0 || state.maxPrice < 99999;
        if (clearAllFiltersBtn) {
            clearAllFiltersBtn.style.display = isFiltered ? 'inline-flex' : 'none';
        }

        // Sorting
        sortProductCards();
    }

    function sortProductCards() {
        if (!productGrid) return;
        const cardsArray = Array.from(productCards);

        cardsArray.sort((a, b) => {
            const priceA = parseFloat(a.getAttribute('data-price') || '0');
            const priceB = parseFloat(b.getAttribute('data-price') || '0');
            const idA = parseInt(a.getAttribute('data-id') || '0', 10);
            const idB = parseInt(b.getAttribute('data-id') || '0', 10);

            if (state.sort === 'price_asc') {
                return priceA - priceB;
            } else if (state.sort === 'price_desc') {
                return priceB - priceA;
            } else {
                // Newest by ID
                return idB - idA;
            }
        });

        cardsArray.forEach(card => productGrid.appendChild(card));
    }

    // Category button events
    categoryPills.forEach(pill => {
        pill.addEventListener('click', (e) => {
            categoryPills.forEach(p => p.classList.remove('active'));
            pill.classList.add('active');
            state.category = pill.getAttribute('data-category') || 'all';
            applyFilters();
        });
    });

    // Live search input
    if (liveSearchInput) {
        liveSearchInput.addEventListener('input', (e) => {
            state.keyword = e.target.value;
            if (clearSearchBtn) {
                clearSearchBtn.style.display = state.keyword ? 'block' : 'none';
            }
            applyFilters();
        });
    }

    if (clearSearchBtn) {
        clearSearchBtn.addEventListener('click', () => {
            liveSearchInput.value = '';
            state.keyword = '';
            clearSearchBtn.style.display = 'none';
            applyFilters();
        });
    }

    // Hero Search Trigger
    if (heroSearchBtn && heroSearchInput) {
        heroSearchBtn.addEventListener('click', () => {
            executeHeroSearch();
        });
        heroSearchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                executeHeroSearch();
            }
        });
    }

    function executeHeroSearch() {
        const val = heroSearchInput.value.trim();
        state.keyword = val;
        if (liveSearchInput) {
            liveSearchInput.value = val;
            if (clearSearchBtn) clearSearchBtn.style.display = val ? 'block' : 'none';
        }
        applyFilters();
        document.getElementById('marketplace-section')?.scrollIntoView({ behavior: 'smooth' });
    }

    // Hero quick tags
    heroTags.forEach(tag => {
        tag.addEventListener('click', () => {
            const query = tag.getAttribute('data-tag') || '';
            if (heroSearchInput) heroSearchInput.value = query;
            if (liveSearchInput) {
                liveSearchInput.value = query;
                if (clearSearchBtn) clearSearchBtn.style.display = 'block';
            }
            state.keyword = query;
            applyFilters();
            document.getElementById('marketplace-section')?.scrollIntoView({ behavior: 'smooth' });
        });
    });

    // Budget range filter chips
    budgetButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            budgetButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            state.minPrice = parseFloat(btn.getAttribute('data-min') || '0');
            state.maxPrice = parseFloat(btn.getAttribute('data-max') || '99999');
            applyFilters();
        });
    });

    // Sort Dropdown
    if (sortDropdown) {
        sortDropdown.addEventListener('change', (e) => {
            state.sort = e.target.value;
            sortProductCards();
        });
    }

    // Clear all filters
    function resetAllFilters() {
        state.category = 'all';
        state.keyword = '';
        state.minPrice = 0;
        state.maxPrice = 99999;
        state.sort = 'newest';

        if (liveSearchInput) liveSearchInput.value = '';
        if (heroSearchInput) heroSearchInput.value = '';
        if (clearSearchBtn) clearSearchBtn.style.display = 'none';

        categoryPills.forEach(p => {
            if (p.getAttribute('data-category') === 'all') {
                p.classList.add('active');
            } else {
                p.classList.remove('active');
            }
        });

        budgetButtons.forEach((b, idx) => {
            if (idx === 0) b.classList.add('active');
            else b.classList.remove('active');
        });

        if (sortDropdown) sortDropdown.value = 'newest';

        applyFilters();
    }

    if (clearAllFiltersBtn) clearAllFiltersBtn.addEventListener('click', resetAllFilters);
    if (resetEmptyStateBtn) resetEmptyStateBtn.addEventListener('click', resetAllFilters);


    // -------------------------------------------------------------
    // 2. WISHLIST MANAGEMENT
    // -------------------------------------------------------------
    function updateWishlistUI() {
        const count = state.wishlist.length;
        if (wishlistBadge) wishlistBadge.textContent = count;

        document.querySelectorAll('.wishlist-btn').forEach(btn => {
            const id = parseInt(btn.getAttribute('data-id') || '0', 10);
            const icon = btn.querySelector('i');
            if (state.wishlist.includes(id)) {
                btn.classList.add('active');
                if (icon) {
                    icon.classList.remove('bi-heart', 'text-muted');
                    icon.classList.add('bi-heart-fill', 'text-danger');
                }
            } else {
                btn.classList.remove('active');
                if (icon) {
                    icon.classList.remove('bi-heart-fill', 'text-danger');
                    icon.classList.add('bi-heart', 'text-muted');
                }
            }
        });
    }

    document.addEventListener('click', (e) => {
        const btn = e.target.closest('.wishlist-btn');
        if (!btn) return;
        e.preventDefault();
        e.stopPropagation();

        const id = parseInt(btn.getAttribute('data-id') || '0', 10);
        if (!id) return;

        const idx = state.wishlist.indexOf(id);
        if (idx > -1) {
            state.wishlist.splice(idx, 1);
        } else {
            state.wishlist.push(id);
        }

        localStorage.setItem('campusloop_wishlist', JSON.stringify(state.wishlist));
        updateWishlistUI();
    });


    // -------------------------------------------------------------
    // 3. QUICK VIEW MODAL (R.2.2 & R.14.2: 3-4 Photos Gallery)
    // -------------------------------------------------------------
    const quickViewModal = new bootstrap.Modal(document.getElementById('quickViewModal'));

    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.quick-view-btn');
        if (!btn) return;
        e.preventDefault();

        const id = btn.getAttribute('data-id');
        if (!id) return;

        try {
            // Call ASP.NET Core JSON endpoint
            const res = await fetch(`/Home/QuickView?id=${id}`);
            const data = await res.json();

            if (data && data.success) {
                populateQuickView(data);
                quickViewModal.show();
            }
        } catch (err) {
            console.error('Failed to load quick view details', err);
        }
    });

    function populateQuickView(data) {
        document.getElementById('modalCategoryBadge').textContent = data.category || 'General';
        document.getElementById('modalStatusBadge').textContent = data.status || 'AVAILABLE';
        document.getElementById('modalConditionBadge').textContent = data.condition || 'Good';
        document.getElementById('modalProductName').textContent = data.name;
        document.getElementById('modalProductPrice').textContent = `₹${Number(data.price).toLocaleString('en-IN')}`;
        document.getElementById('modalCampusLocation').textContent = data.location || 'Campus Library';
        document.getElementById('modalProductDescription').textContent = data.description || '';

        // Seller details (R.7.1)
        if (data.seller) {
            document.getElementById('modalSellerName').textContent = data.seller.name || 'Verified Student';
            document.getElementById('modalSellerBranch').textContent = `${data.seller.branch} • ${data.seller.semester}`;
            document.getElementById('modalSellerPhone').innerHTML = `<i class="bi bi-telephone me-1"></i> ${data.seller.phone} &bull; <i class="bi bi-envelope ms-1 me-1"></i> ${data.seller.email}`;
            document.getElementById('modalSellerAvatar').src = data.seller.avatar || 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100';
        }

        // Image Gallery (R.14.2: Multiple Images)
        const mainImg = document.getElementById('modalMainImage');
        const thumbStrip = document.getElementById('modalThumbnailStrip');
        thumbStrip.innerHTML = '';

        if (data.images && data.images.length > 0) {
            mainImg.src = data.images[0];

            data.images.forEach((url, i) => {
                const thumb = document.createElement('img');
                thumb.src = url;
                thumb.className = `modal-thumb-img rounded-2 border cursor-pointer ${i === 0 ? 'active border-primary' : ''}`;
                thumb.style.width = '64px';
                thumb.style.height = '64px';
                thumb.style.objectFit = 'cover';
                thumb.alt = `Thumbnail ${i + 1}`;

                thumb.addEventListener('click', () => {
                    mainImg.src = url;
                    thumbStrip.querySelectorAll('img').forEach(t => t.classList.remove('active', 'border-primary'));
                    thumb.classList.add('active', 'border-primary');
                });

                thumbStrip.appendChild(thumb);
            });
        }

        // Connect Quick View "Buy Now" to Payment Modal
        const buyNowBtn = document.getElementById('modalBuyNowBtn');
        if (buyNowBtn) {
            buyNowBtn.onclick = () => {
                quickViewModal.hide();
                setTimeout(() => {
                    openPaymentModal(data.id);
                }, 350);
            };
        }

        // Connect Quick View "Start Chat" to Chat Modal
        const startChatBtn = document.getElementById('modalStartChatBtn');
        if (startChatBtn) {
            startChatBtn.onclick = () => {
                quickViewModal.hide();
                setTimeout(() => {
                    openChatModal({
                        id: data.id,
                        name: data.name,
                        price: data.price,
                        image: data.images && data.images[0] ? data.images[0] : '',
                        seller: data.seller ? data.seller.name : 'Student',
                        branch: data.seller ? data.seller.branch : '',
                        semester: data.seller ? data.seller.semester : ''
                    });
                }, 350);
            };
        }
    }


    // -------------------------------------------------------------
    // 4. REAL-TIME CHAT INTERACTION (R.6.1, R.6.2, R.6.3, R.6.5)
    // -------------------------------------------------------------
    const chatModalEl = document.getElementById('chatModal');
    const chatModal = new bootstrap.Modal(chatModalEl);
    const chatSendForm = document.getElementById('chatSendForm');
    const chatMessageInput = document.getElementById('chatMessageInput');
    const chatMessagesContainer = document.getElementById('chatMessagesContainer');

    function openChatModal(info) {
        state.activeProductChat = info;

        // Populate context banner in chat (R.6.5)
        document.getElementById('chatSellerName').textContent = info.seller || 'Seller';
        document.getElementById('chatSellerStatus').textContent = `Online | ${info.branch || 'Campus Student'}`;
        document.getElementById('chatProductName').textContent = info.name || 'Campus Item';
        document.getElementById('chatProductPrice').textContent = `₹${Number(info.price).toLocaleString('en-IN')}`;
        document.getElementById('chatProductThumb').src = info.image || 'https://images.unsplash.com/photo-1594980596870-8aa52a78d8cd?w=100';

        chatModal.show();
    }

    // Trigger chat from product card
    document.addEventListener('click', (e) => {
        const btn = e.target.closest('.chat-trigger-btn');
        if (!btn) return;
        e.preventDefault();

        const info = {
            id: btn.getAttribute('data-id'),
            name: btn.getAttribute('data-name'),
            price: btn.getAttribute('data-price'),
            image: btn.getAttribute('data-image'),
            seller: btn.getAttribute('data-seller'),
            branch: btn.getAttribute('data-branch'),
            semester: btn.getAttribute('data-semester')
        };

        openChatModal(info);
    });

    // Send Message in Chat (R.6.2 & R.6.3)
    if (chatSendForm) {
        chatSendForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const text = chatMessageInput.value.trim();
            if (!text) return;

            const timeStr = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

            // Append sent message bubble
            const sentMsgEl = document.createElement('div');
            sentMsgEl.className = 'chat-msg sent d-flex flex-column align-items-end mb-3';
            sentMsgEl.innerHTML = `
                <div class="msg-bubble p-2 px-3 rounded-4 bg-primary text-white shadow-xs small">
                    ${escapeHtml(text)}
                </div>
                <span class="fs-9 text-muted mt-1 me-2">${timeStr} &bull; <i class="bi bi-check2 text-muted"></i></span>
            `;
            chatMessagesContainer.appendChild(sentMsgEl);
            chatMessageInput.value = '';
            chatMessagesContainer.scrollTop = chatMessagesContainer.scrollHeight;

            // Update checkmark after 400ms to simulate delivery
            setTimeout(() => {
                const tick = sentMsgEl.querySelector('i');
                if (tick) {
                    tick.className = 'bi bi-check2-all text-primary';
                }
            }, 400);

            // Simulate Real-time SignalR Seller Reply after 1.4s (R.6.3)
            setTimeout(() => {
                const replyTime = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
                const receivedMsgEl = document.createElement('div');
                receivedMsgEl.className = 'chat-msg received d-flex flex-column align-items-start mb-3';
                receivedMsgEl.innerHTML = `
                    <div class="msg-bubble p-2 px-3 rounded-4 bg-white border shadow-xs text-dark small">
                        Got your message! Deal confirmed. Let's meet at the library lawn at that time. Thank you!
                    </div>
                    <span class="fs-9 text-muted mt-1 ms-2">${replyTime}</span>
                `;
                chatMessagesContainer.appendChild(receivedMsgEl);
                chatMessagesContainer.scrollTop = chatMessagesContainer.scrollHeight;
            }, 1400);
        });
    }


    // -------------------------------------------------------------
    // 5. POST PRODUCT HANDLER (R.3.1)
    // -------------------------------------------------------------
    const postProductForm = document.getElementById('postProductForm');
    if (postProductForm) {
        postProductForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const name = document.getElementById('postName').value.trim();
            const category = document.getElementById('postCategory').value;
            const price = parseFloat(document.getElementById('postPrice').value);
            const condition = document.getElementById('postCondition').value;
            const location = document.getElementById('postLocation').value.trim();
            const description = document.getElementById('postDescription').value.trim();

            // Default cover image based on category
            let coverImg = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=600";
            if (category === 'tech') coverImg = "https://images.unsplash.com/photo-1594980596870-8aa52a78d8cd?w=600";
            else if (category === 'books') coverImg = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600";
            else if (category === 'hostel') coverImg = "https://images.unsplash.com/photo-1518455027359-f3f8164ba6bd?w=600";
            else if (category === 'mobility') coverImg = "https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=600";

            const newId = Date.now();

            // Create new card dynamically and prepend to grid
            const col = document.createElement('div');
            col.className = 'col-12 col-sm-6 col-md-4 col-lg-3 product-card-col';
            col.setAttribute('data-id', newId);
            col.setAttribute('data-name', name.toLowerCase());
            col.setAttribute('data-category', category);
            col.setAttribute('data-price', price);
            col.setAttribute('data-condition', condition);
            col.setAttribute('data-status', 'AVAILABLE');

            col.innerHTML = `
                <div class="card h-100 border-0 shadow-sm product-card rounded-4 overflow-hidden position-relative">
                    <div class="product-img-wrapper position-relative">
                        <img src="${coverImg}" class="card-img-top product-img" alt="${escapeHtml(name)}" loading="lazy" />
                        <span class="badge bg-success position-absolute top-0 start-0 m-3 shadow-xs">
                            <i class="bi bi-check-circle-fill me-1"></i> AVAILABLE
                        </span>
                        <span class="badge bg-dark bg-opacity-75 text-white position-absolute bottom-0 start-0 m-3 backdrop-blur small">
                            ${condition}
                        </span>
                        <button type="button" class="btn btn-white btn-sm rounded-circle position-absolute top-0 end-0 m-3 shadow-sm wishlist-btn" 
                                data-id="${newId}" title="Add to Wishlist">
                            <i class="bi bi-heart text-muted"></i>
                        </button>
                    </div>
                    <div class="card-body p-3 d-flex flex-column">
                        <div class="d-flex justify-content-between align-items-center mb-1 small">
                            <span class="badge bg-primary-subtle text-primary fw-semibold rounded-pill px-2 py-1">
                                Just Posted
                            </span>
                            <span class="text-muted fs-8"><i class="bi bi-clock me-1"></i>Now</span>
                        </div>
                        <h6 class="card-title fw-bold text-dark mb-1 product-title text-truncate-2" title="${escapeHtml(name)}">
                            ${escapeHtml(name)}
                        </h6>
                        <div class="text-muted small mb-2 d-flex align-items-center text-truncate">
                            <i class="bi bi-geo-alt text-danger me-1 flex-shrink-0"></i>
                            <span class="text-truncate">${escapeHtml(location)}</span>
                        </div>
                        <div class="d-flex align-items-baseline gap-2 mb-3 mt-auto">
                            <span class="fs-4 fw-extrabold text-dark product-price">₹${price.toLocaleString('en-IN')}</span>
                            <span class="badge bg-light text-muted border small">Negotiable</span>
                        </div>
                        <div class="seller-banner p-2 rounded-3 bg-light d-flex align-items-center justify-content-between mb-3">
                            <div class="d-flex align-items-center gap-2 overflow-hidden">
                                <img src="https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=100" class="rounded-circle" width="28" height="28">
                                <div class="text-truncate">
                                    <div class="fw-bold fs-8 text-dark">Aarav Patel (You)</div>
                                    <div class="text-muted fs-9">Comp. Engg (Sem 5)</div>
                                </div>
                            </div>
                        </div>
                        <div class="row g-2 pt-1 border-top mt-1">
                            <div class="col-7">
                                <button type="button" class="btn btn-primary btn-sm w-100 rounded-3 fw-semibold d-flex align-items-center justify-content-center gap-1 chat-trigger-btn"
                                        data-id="${newId}" data-name="${escapeHtml(name)}" data-price="${price}" data-image="${coverImg}" data-seller="Aarav Patel" data-branch="Comp. Engg" data-semester="Sem 5">
                                    <i class="bi bi-chat-dots-fill"></i>
                                    <span>Chat & Offer</span>
                                </button>
                            </div>
                            <div class="col-5">
                                <button type="button" class="btn btn-outline-secondary btn-sm w-100 rounded-3" onclick="alert('Item Details: ${escapeHtml(name)}\\nPrice: ₹${price}\\nHandover: ${escapeHtml(location)}')">
                                    <i class="bi bi-info-circle me-1"></i> Details
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            productGrid.prepend(col);

            // Close modal
            bootstrap.Modal.getInstance(document.getElementById('postProductModal'))?.hide();
            postProductForm.reset();

            // Notify user
            alert(`🎉 Success! "${name}" has been posted to the CampusLoop marketplace.`);
            applyFilters();
            col.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    // -------------------------------------------------------------
    // 6. CAMPUSLOOP SAFE PAYMENT & CHECKOUT SYSTEM
    // -------------------------------------------------------------
    const paymentModalEl = document.getElementById('paymentModal');
    const paymentModal = paymentModalEl ? new bootstrap.Modal(paymentModalEl) : null;
    let activePaymentProduct = null;

    window.openPaymentModal = async function(productId) {
        if (!productId) return;
        try {
            const res = await fetch(`/Payment/GetPaymentDetails?productId=${productId}`);
            const data = await res.json();
            if (!data || !data.success) {
                alert('Could not retrieve payment details for this product.');
                return;
            }

            activePaymentProduct = data;

            // Reset modal screen views
            document.getElementById('paymentCheckoutBody').style.display = 'block';
            document.getElementById('paymentLoadingScreen').style.display = 'none';
            document.getElementById('paymentSuccessScreen').style.display = 'none';

            // Populate Order Summary
            document.getElementById('payProductName').textContent = data.productName;
            document.getElementById('payProductCondition').textContent = data.condition || 'Good';
            document.getElementById('payProductLocation').textContent = data.location || 'Library Lawn';
            document.getElementById('payProductThumb').src = data.image || 'https://images.unsplash.com/photo-1594980596870-8aa52a78d8cd?w=100';
            document.getElementById('paySummaryPrice').textContent = `₹${Number(data.price).toLocaleString('en-IN')}`;
            document.getElementById('paySummaryTotal').textContent = `₹${Number(data.price).toLocaleString('en-IN')}`;

            if (data.seller) {
                document.getElementById('paySellerName').textContent = data.seller.name || 'Campus Student';
                document.getElementById('paySellerBranch').textContent = `${data.seller.branch || ''} • ${data.seller.semester || ''}`;
                document.getElementById('paySellerUpiId').value = data.seller.upiId || 'seller@okddu';
            }

            // Generate dynamic UPI QR code
            const upiString = `upi://pay?pa=${encodeURIComponent(data.seller?.upiId || 'seller@okddu')}&pn=${encodeURIComponent(data.seller?.name || 'Seller')}&am=${data.price}&cu=INR`;
            const qrUrl = `https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encodeURIComponent(upiString)}`;
            document.getElementById('payQrCodeImg').src = qrUrl;

            // Show modal
            if (paymentModal) paymentModal.show();
        } catch (err) {
            console.error('Failed to open payment modal', err);
        }
    };

    // Trigger payment from product cards
    document.addEventListener('click', (e) => {
        const btn = e.target.closest('.pay-trigger-btn');
        if (!btn) return;
        e.preventDefault();
        const id = btn.getAttribute('data-id');
        openPaymentModal(id);
    });

    // Trigger payment from Chat modal
    const chatPayNowBtn = document.getElementById('chatPayNowBtn');
    if (chatPayNowBtn) {
        chatPayNowBtn.addEventListener('click', () => {
            chatModal.hide();
            setTimeout(() => {
                if (state.activeProductChat && state.activeProductChat.id) {
                    openPaymentModal(state.activeProductChat.id);
                }
            }, 350);
        });
    }

    // Copy UPI ID button
    const copyUpiIdBtn = document.getElementById('copyUpiIdBtn');
    if (copyUpiIdBtn) {
        copyUpiIdBtn.addEventListener('click', () => {
            const upiIdInput = document.getElementById('paySellerUpiId');
            if (upiIdInput) {
                navigator.clipboard.writeText(upiIdInput.value).then(() => {
                    copyUpiIdBtn.innerHTML = '<i class="bi bi-check-lg text-success"></i>';
                    setTimeout(() => {
                        copyUpiIdBtn.innerHTML = '<i class="bi bi-clipboard"></i>';
                    }, 2000);
                });
            }
        });
    }

    // Process Payment API Call
    async function executePayment(method, payload = {}) {
        if (!activePaymentProduct) return;

        // Switch to loading screen
        document.getElementById('paymentCheckoutBody').style.display = 'none';
        document.getElementById('paymentLoadingScreen').style.display = 'block';

        const requestBody = {
            productId: activePaymentProduct.productId,
            paymentMethod: method,
            amount: activePaymentProduct.price,
            ...payload
        };

        try {
            const res = await fetch('/Payment/ProcessPayment', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(requestBody)
            });
            const result = await res.json();

            // Simulate banking delay for realistic UX
            setTimeout(() => {
                document.getElementById('paymentLoadingScreen').style.display = 'none';
                if (result && result.success) {
                    showPaymentSuccess(result);
                } else {
                    alert(result?.message || 'Payment processing failed');
                    document.getElementById('paymentCheckoutBody').style.display = 'block';
                }
            }, 1000);
        } catch (err) {
            console.error('Payment request error', err);
            document.getElementById('paymentLoadingScreen').style.display = 'none';
            document.getElementById('paymentCheckoutBody').style.display = 'block';
            alert('Error processing transaction. Please try again.');
        }
    }

    function showPaymentSuccess(data) {
        document.getElementById('paymentSuccessScreen').style.display = 'block';
        document.getElementById('receiptSuccessTitle').textContent = data.paymentMethod === 'CashOnHandover' 
            ? 'Handover Meeting Confirmed!' 
            : 'Payment Successful!';
        document.getElementById('receiptSuccessSubtitle').textContent = data.message;
        document.getElementById('receiptStatusBadge').textContent = data.paymentMethod === 'CashOnHandover'
            ? 'CASH ON HANDOVER • CONFIRMED'
            : 'PAID • SETTLED';
        document.getElementById('receiptTxnId').textContent = data.transactionId;
        document.getElementById('receiptOrderId').textContent = data.orderId;
        document.getElementById('receiptProductName').textContent = data.productName;
        document.getElementById('receiptAmount').textContent = `₹${Number(data.amount).toLocaleString('en-IN')}`;
        document.getElementById('receiptSeller').textContent = data.sellerName;
        document.getElementById('receiptLocation').textContent = data.meetingLocation;
        document.getElementById('receiptOtp').textContent = data.handshakeOtp;

        // Mark the item card as PAID/SOLD on the UI
        const cardCol = document.querySelector(`.product-card-col[data-id="${data.productId}"]`);
        if (cardCol) {
            const statusBadge = cardCol.querySelector('.badge.bg-success');
            if (statusBadge) {
                statusBadge.textContent = 'SOLD / RESERVED';
                statusBadge.className = 'badge bg-secondary position-absolute top-0 start-0 m-3 shadow-xs';
            }
            const payBtn = cardCol.querySelector('.pay-trigger-btn');
            if (payBtn) {
                payBtn.disabled = true;
                payBtn.className = 'btn btn-secondary btn-sm w-100 rounded-3 fw-bold';
                payBtn.innerHTML = '<i class="bi bi-check2-circle"></i> Deal Finalized';
            }
        }
    }

    // Handle payment button submissions
    document.getElementById('submitUpiPaymentBtn')?.addEventListener('click', () => {
        executePayment('UpiQr');
    });

    document.getElementById('cardPaymentForm')?.addEventListener('submit', (e) => {
        e.preventDefault();
        const cardNum = document.getElementById('cardNumInput').value;
        executePayment('Card', { cardNumber: cardNum });
    });

    document.getElementById('submitNetBankingBtn')?.addEventListener('click', () => {
        const activeBank = document.querySelector('.bank-select-card.active')?.getAttribute('data-bank') || 'State Bank of India';
        executePayment('NetBanking', { bankName: activeBank });
    });

    document.getElementById('submitCashPaymentBtn')?.addEventListener('click', () => {
        executePayment('CashOnHandover');
    });

    // Bank Selection Cards
    document.querySelectorAll('.bank-select-card').forEach(card => {
        card.addEventListener('click', () => {
            document.querySelectorAll('.bank-select-card').forEach(c => c.classList.remove('active', 'border-primary', 'bg-primary-subtle'));
            card.classList.add('active', 'border-primary', 'bg-primary-subtle');
        });
    });

    // Done button in receipt
    document.getElementById('receiptDoneBtn')?.addEventListener('click', () => {
        if (paymentModal) paymentModal.hide();
    });

    // Helper: HTML Escaping
    function escapeHtml(str) {
        return str.replace(/[&<>'"]/g, 
            tag => ({
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                "'": '&#39;',
                '"': '&quot;'
            }[tag] || tag)
        );
    }
});

// Global Function: Mark as Sold Demo (R.3.5)
window.markAsSoldDemo = function(id) {
    const statusBadge = document.getElementById(`myProdStatus${id}`);
    if (confirm("Are you sure you want to mark this item as SOLD? It will be removed from active marketplace listings (R.3.5).")) {
        if (statusBadge) {
            statusBadge.textContent = "SOLD";
            statusBadge.className = "badge bg-secondary";
        }
        alert("✅ Product marked as SOLD successfully! It is now archived in your product history.");
    }
};
