import{G as e,H as t,K as n,W as r,d as i,f as a,k as o,nt as s,p as c,r as l,tt as u,u as d,v as f,z as p}from"./DxfkHh_f.js";import{t as ee}from"./D5TQhe1R.js";import{E as m,H as h,I as g,P as _,T as v,mt as te,nt as y,r as ne,t as b,vt as re,z as ie}from"./Cxvj1CVF.js";import{t as x}from"./cO8iUN-n.js";import{t as S}from"./DCLXnpaw.js";import{t as C}from"./B8PQWoeu.js";import{t as w}from"./BvzTRszk.js";import{t as ae}from"./Bbd-WAcs.js";import{n as T,t as oe}from"./DDHEri59.js";import{t as se}from"./2xszPtiz.js";import{t as E}from"./C2-R9xpQ.js";import{t as ce}from"./C7uv8j4I.js";import{t as D}from"./Bxs-1YSe.js";var le=ne.extend({name:`galleria`,style:`
    .p-galleria {
        overflow: hidden;
        border-style: solid;
        border-width: dt('galleria.border.width');
        border-color: dt('galleria.border.color');
        border-radius: dt('galleria.border.radius');
    }

    .p-galleria-content {
        display: flex;
        flex-direction: column;
    }

    .p-galleria-items-container {
        display: flex;
        flex-direction: column;
        position: relative;
    }

    .p-galleria-items {
        position: relative;
        display: flex;
        height: 100%;
    }

    .p-galleria-nav-button {
        position: absolute !important;
        top: 50%;
        display: inline-flex;
        justify-content: center;
        align-items: center;
        overflow: hidden;
        background: dt('galleria.nav.button.background');
        color: dt('galleria.nav.button.color');
        width: dt('galleria.nav.button.size');
        height: dt('galleria.nav.button.size');
        transition:
            background dt('galleria.transition.duration'),
            color dt('galleria.transition.duration'),
            outline-color dt('galleria.transition.duration'),
            box-shadow dt('galleria.transition.duration');
        margin: calc(-1 * calc(dt('galleria.nav.button.size')) / 2) dt('galleria.nav.button.gutter') 0 dt('galleria.nav.button.gutter');
        padding: 0;
        user-select: none;
        border: 0 none;
        cursor: pointer;
        outline-color: transparent;
    }

    .p-galleria-nav-button:not(.p-disabled):hover {
        background: dt('galleria.nav.button.hover.background');
        color: dt('galleria.nav.button.hover.color');
    }

    .p-galleria-nav-button:not(.p-disabled):focus-visible {
        box-shadow: dt('galleria.nav.button.focus.ring.shadow');
        outline: dt('galleria.nav.button.focus.ring.width') dt('galleria.nav.button.focus.ring.style') dt('galleria.nav.button.focus.ring.color');
        outline-offset: dt('galleria.nav.button.focus.ring.offset');
    }

    .p-galleria-next-icon,
    .p-galleria-prev-icon {
        font-size: dt('galleria.nav.icon.size');
        width: dt('galleria.nav.icon.size');
        height: dt('galleria.nav.icon.size');
    }

    .p-galleria-prev-button {
        border-radius: dt('galleria.nav.button.prev.border.radius');
        left: 0;
    }

    .p-galleria-next-button {
        border-radius: dt('galleria.nav.button.next.border.radius');
        right: 0;
    }

    .p-galleria-prev-button:dir(rtl) {
        left: auto;
        right: 0;
        transform: rotate(180deg);
    }

    .p-galleria-next-button:dir(rtl) {
        right: auto;
        left: 0;
        transform: rotate(180deg);
    }

    .p-galleria-item {
        display: flex;
        justify-content: center;
        align-items: center;
        height: 100%;
        width: 100%;
    }

    .p-galleria-hover-navigators .p-galleria-nav-button {
        pointer-events: none;
        opacity: 0;
        transition: opacity dt('galleria.transition.duration') ease-in-out;
    }

    .p-galleria-hover-navigators .p-galleria-items-container:hover .p-galleria-nav-button {
        pointer-events: all;
        opacity: 1;
    }

    .p-galleria-hover-navigators .p-galleria-items-container:hover .p-galleria-nav-button.p-disabled {
        pointer-events: none;
    }

    .p-galleria-caption {
        position: absolute;
        bottom: 0;
        left: 0;
        width: 100%;
        background: dt('galleria.caption.background');
        color: dt('galleria.caption.color');
        padding: dt('galleria.caption.padding');
    }

    .p-galleria-thumbnails {
        display: flex;
        flex-direction: column;
        overflow: auto;
        flex-shrink: 0;
    }

    .p-galleria-thumbnail-nav-button {
        align-self: center;
        flex: 0 0 auto;
        display: flex;
        justify-content: center;
        align-items: center;
        overflow: hidden;
        position: relative;
        margin: 0 dt('galleria.thumbnail.nav.button.gutter');
        padding: 0;
        border: none;
        user-select: none;
        cursor: pointer;
        background: transparent;
        color: dt('galleria.thumbnail.nav.button.color');
        width: dt('galleria.thumbnail.nav.button.size');
        height: dt('galleria.thumbnail.nav.button.size');
        transition:
            background dt('galleria.transition.duration'),
            color dt('galleria.transition.duration'),
            outline-color dt('galleria.transition.duration');
        outline-color: transparent;
        border-radius: dt('galleria.thumbnail.nav.button.border.radius');
    }

    .p-galleria-thumbnail-nav-button:hover {
        background: dt('galleria.thumbnail.nav.button.hover.background');
        color: dt('galleria.thumbnail.nav.button.hover.color');
    }

    .p-galleria-thumbnail-nav-button:focus-visible {
        box-shadow: dt('galleria.thumbnail.nav.button.focus.ring.shadow');
        outline: dt('galleria.thumbnail.nav.button.focus.ring.width') dt('galleria.thumbnail.nav.button.focus.ring.style') dt('galleria.thumbnail.nav.button.focus.ring.color');
        outline-offset: dt('galleria.thumbnail.nav.button.focus.ring.offset');
    }

    .p-galleria-thumbnail-nav-button .p-galleria-thumbnail-next-icon,
    .p-galleria-thumbnail-nav-button .p-galleria-thumbnail-prev-icon {
        font-size: dt('galleria.thumbnail.nav.button.icon.size');
        width: dt('galleria.thumbnail.nav.button.icon.size');
        height: dt('galleria.thumbnail.nav.button.icon.size');
    }

    .p-galleria-thumbnails-content {
        display: flex;
        flex-direction: row;
        background: dt('galleria.thumbnails.content.background');
        padding: dt('galleria.thumbnails.content.padding');
    }

    .p-galleria-thumbnails-viewport {
        overflow: hidden;
        width: 100%;
    }

    .p-galleria:not(.p-galleria-thumbnails-right):not(.p-galleria-thumbnails-left) .p-galleria-thumbnail-prev-button:dir(rtl),
    .p-galleria:not(.p-galleria-thumbnails-right):not(.p-galleria-thumbnails-left) .p-galleria-thumbnail-next-button:dir(rtl) {
        transform: rotate(180deg);
    }

    .p-galleria-thumbnail-items {
        display: flex;
    }

    .p-galleria-thumbnail-items:dir(rtl) {
        flex-direction: row-reverse;
    }

    .p-galleria-thumbnail-item {
        overflow: auto;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        opacity: 0.5;
    }

    .p-galleria-thumbnail {
        outline-color: transparent;
    }

    .p-galleria-thumbnail-item:hover {
        opacity: 1;
        transition: opacity 0.3s;
    }

    .p-galleria-thumbnail-item-current {
        opacity: 1;
    }

    .p-galleria-thumbnails-left .p-galleria-content,
    .p-galleria-thumbnails-right .p-galleria-content {
        flex-direction: row;
    }

    .p-galleria-thumbnails-left .p-galleria-items-container,
    .p-galleria-thumbnails-right .p-galleria-items-container {
        flex-direction: row;
    }

    .p-galleria-thumbnails-left .p-galleria-items-container,
    .p-galleria-thumbnails-top .p-galleria-items-container {
        order: 2;
    }

    .p-galleria-thumbnails-left .p-galleria-thumbnails,
    .p-galleria-thumbnails-top .p-galleria-thumbnails {
        order: 1;
    }

    .p-galleria-thumbnails-left .p-galleria-thumbnails-content,
    .p-galleria-thumbnails-right .p-galleria-thumbnails-content {
        flex-direction: column;
        flex-grow: 1;
    }

    .p-galleria-thumbnails-left .p-galleria-thumbnail-items,
    .p-galleria-thumbnails-right .p-galleria-thumbnail-items {
        flex-direction: column;
        height: 100%;
    }

    .p-galleria-indicator-list {
        display: flex;
        align-items: center;
        justify-content: center;
        padding: dt('galleria.indicator.list.padding');
        gap: dt('galleria.indicator.list.gap');
        margin: 0;
        list-style: none;
    }

    .p-galleria-indicator-button {
        display: inline-flex;
        align-items: center;
        background: dt('galleria.indicator.button.background');
        width: dt('galleria.indicator.button.width');
        height: dt('galleria.indicator.button.height');
        transition:
            background dt('galleria.transition.duration'),
            color dt('galleria.transition.duration'),
            outline-color dt('galleria.transition.duration'),
            box-shadow dt('galleria.transition.duration');
        outline-color: transparent;
        border-radius: dt('galleria.indicator.button.border.radius');
        margin: 0;
        padding: 0;
        border: none;
        user-select: none;
        cursor: pointer;
    }

    .p-galleria-indicator-button:hover {
        background: dt('galleria.indicator.button.hover.background');
    }

    .p-galleria-indicator-button:focus-visible {
        box-shadow: dt('galleria.indicator.button.focus.ring.shadow');
        outline: dt('galleria.indicator.button.focus.ring.width') dt('galleria.indicator.button.focus.ring.style') dt('galleria.indicator.button.focus.ring.color');
        outline-offset: dt('galleria.indicator.button.focus.ring.offset');
    }

    .p-galleria-indicator-active .p-galleria-indicator-button {
        background: dt('galleria.indicator.button.active.background');
    }

    .p-galleria-indicators-left .p-galleria-items-container,
    .p-galleria-indicators-right .p-galleria-items-container {
        flex-direction: row;
        align-items: center;
    }

    .p-galleria-indicators-left .p-galleria-items,
    .p-galleria-indicators-top .p-galleria-items {
        order: 2;
    }

    .p-galleria-indicators-left .p-galleria-indicator-list,
    .p-galleria-indicators-top .p-galleria-indicator-list {
        order: 1;
    }

    .p-galleria-indicators-left .p-galleria-indicator-list,
    .p-galleria-indicators-right .p-galleria-indicator-list {
        flex-direction: column;
    }

    .p-galleria-inset-indicators .p-galleria-indicator-list {
        position: absolute;
        display: flex;
        z-index: 1;
        background: dt('galleria.inset.indicator.list.background');
    }

    .p-galleria-inset-indicators .p-galleria-indicator-button {
        background: dt('galleria.inset.indicator.button.background');
    }

    .p-galleria-inset-indicators .p-galleria-indicator-button:hover {
        background: dt('galleria.inset.indicator.button.hover.background');
    }

    .p-galleria-inset-indicators .p-galleria-indicator-active .p-galleria-indicator-button {
        background: dt('galleria.inset.indicator.button.active.background');
    }

    .p-galleria-inset-indicators.p-galleria-indicators-top .p-galleria-indicator-list {
        top: 0;
        left: 0;
        width: 100%;
        align-items: flex-start;
    }

    .p-galleria-inset-indicators.p-galleria-indicators-right .p-galleria-indicator-list {
        right: 0;
        top: 0;
        height: 100%;
        align-items: flex-end;
    }

    .p-galleria-inset-indicators.p-galleria-indicators-bottom .p-galleria-indicator-list {
        bottom: 0;
        left: 0;
        width: 100%;
        align-items: flex-end;
    }

    .p-galleria-inset-indicators.p-galleria-indicators-left .p-galleria-indicator-list {
        left: 0;
        top: 0;
        height: 100%;
        align-items: flex-start;
    }

    .p-galleria-mask {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
    }

    .p-galleria-close-button {
        position: absolute !important;
        top: 0;
        right: 0;
        display: flex;
        justify-content: center;
        align-items: center;
        overflow: hidden;
        margin: dt('galleria.close.button.gutter');
        background: dt('galleria.close.button.background');
        color: dt('galleria.close.button.color');
        width: dt('galleria.close.button.size');
        height: dt('galleria.close.button.size');
        padding: 0;
        border: none;
        user-select: none;
        cursor: pointer;
        border-radius: dt('galleria.close.button.border.radius');
        outline-color: transparent;
        transition:
            background dt('galleria.transition.duration'),
            color dt('galleria.transition.duration'),
            outline-color dt('galleria.transition.duration');
    }

    .p-galleria-close-icon {
        font-size: dt('galleria.close.button.icon.size');
        width: dt('galleria.close.button.icon.size');
        height: dt('galleria.close.button.icon.size');
    }

    .p-galleria-close-button:hover {
        background: dt('galleria.close.button.hover.background');
        color: dt('galleria.close.button.hover.color');
    }

    .p-galleria-close-button:focus-visible {
        box-shadow: dt('galleria.close.button.focus.ring.shadow');
        outline: dt('galleria.close.button.focus.ring.width') dt('galleria.close.button.focus.ring.style') dt('galleria.close.button.focus.ring.color');
        outline-offset: dt('galleria.close.button.focus.ring.offset');
    }

    .p-galleria-mask .p-galleria-nav-button {
        position: fixed;
        top: 50%;
    }

       .p-items-hidden .p-galleria-thumbnail-item {
        visibility: hidden;
    }

    .p-items-hidden .p-galleria-thumbnail-item.p-galleria-thumbnail-item-active {
        visibility: visible;
    }

    .p-galleria-enter-active {
        animation: p-animate-galleria-enter 300ms cubic-bezier(.19,1,.22,1);
    }

    .p-galleria-leave-active {
        animation: p-animate-galleria-leave 300ms cubic-bezier(.19,1,.22,1);
    }

    .p-galleria-enter-active .p-galleria-nav-button {
        opacity: 0;
    }

    @keyframes p-animate-galleria-enter {
        from {
            opacity: 0;
            transform: scale(0.93);
        }
    }

    @keyframes p-animate-galleria-leave {
        from {
            opacity: 1;
            transform: scale(1);
        }
        to {
            opacity: 0;
            transform: scale(0.93);
        }
    }
`,classes:{mask:`p-galleria-mask p-overlay-mask p-overlay-mask-enter-active`,root:function(e){var t=e.instance,n=t.$attrs.showThumbnails&&t.getPositionClass(`p-galleria-thumbnails`,t.$attrs.thumbnailsPosition),r=t.$attrs.showIndicators&&t.getPositionClass(`p-galleria-indicators`,t.$attrs.indicatorsPosition);return[`p-galleria p-component`,{"p-galleria-fullscreen":t.$attrs.fullScreen,"p-galleria-inset-indicators":t.$attrs.showIndicatorsOnItem,"p-galleria-hover-navigators":t.$attrs.showItemNavigatorsOnHover&&!t.$attrs.fullScreen},n,r]},closeButton:`p-galleria-close-button`,closeIcon:`p-galleria-close-icon`,header:`p-galleria-header`,content:`p-galleria-content`,footer:`p-galleria-footer`,itemsContainer:`p-galleria-items-container`,items:`p-galleria-items`,prevButton:function(e){return[`p-galleria-prev-button p-galleria-nav-button`,{"p-disabled":e.instance.isNavBackwardDisabled}]},prevIcon:`p-galleria-prev-icon`,item:`p-galleria-item`,nextButton:function(e){return[`p-galleria-next-button p-galleria-nav-button`,{"p-disabled":e.instance.isNavForwardDisabled}]},nextIcon:`p-galleria-next-icon`,caption:`p-galleria-caption`,indicatorList:`p-galleria-indicator-list`,indicator:function(e){var t=e.instance,n=e.index;return[`p-galleria-indicator`,{"p-galleria-indicator-active":t.isIndicatorItemActive(n)}]},indicatorButton:`p-galleria-indicator-button`,thumbnails:`p-galleria-thumbnails`,thumbnailContent:`p-galleria-thumbnails-content`,thumbnailPrevButton:function(e){return[`p-galleria-thumbnail-prev-button p-galleria-thumbnail-nav-button`,{"p-disabled":e.instance.isNavBackwardDisabled}]},thumbnailPrevIcon:`p-galleria-thumbnail-prev-icon`,thumbnailsViewport:`p-galleria-thumbnails-viewport`,thumbnailItems:`p-galleria-thumbnail-items`,thumbnailItem:function(e){var t=e.instance,n=e.index;return[`p-galleria-thumbnail-item`,{"p-galleria-thumbnail-item-current":e.activeIndex===n,"p-galleria-thumbnail-item-active":t.isItemActive(n),"p-galleria-thumbnail-item-start":t.firstItemAciveIndex()===n,"p-galleria-thumbnail-item-end":t.lastItemActiveIndex()===n}]},thumbnail:`p-galleria-thumbnail`,thumbnailNextButton:function(e){return[`p-galleria-thumbnail-next-button p-galleria-thumbnail-nav-button`,{"p-disabled":e.instance.isNavForwardDisabled}]},thumbnailNextIcon:`p-galleria-thumbnail-next-icon`}}),ue={name:`BaseGalleria`,extends:b,props:{id:{type:String,default:null},value:{type:Array,default:null},activeIndex:{type:Number,default:0},fullScreen:{type:Boolean,default:!1},visible:{type:Boolean,default:!1},numVisible:{type:Number,default:3},responsiveOptions:{type:Array,default:null},showItemNavigators:{type:Boolean,default:!1},showThumbnailNavigators:{type:Boolean,default:!0},showItemNavigatorsOnHover:{type:Boolean,default:!1},changeItemOnIndicatorHover:{type:Boolean,default:!1},circular:{type:Boolean,default:!1},autoPlay:{type:Boolean,default:!1},transitionInterval:{type:Number,default:4e3},showThumbnails:{type:Boolean,default:!0},thumbnailsPosition:{type:String,default:`bottom`},verticalThumbnailViewPortHeight:{type:String,default:`300px`},showIndicators:{type:Boolean,default:!1},showIndicatorsOnItem:{type:Boolean,default:!1},indicatorsPosition:{type:String,default:`bottom`},baseZIndex:{type:Number,default:0},maskClass:{type:String,default:null},containerStyle:{type:null,default:null},containerClass:{type:null,default:null},containerProps:{type:null,default:null},prevButtonProps:{type:null,default:null},nextButtonProps:{type:null,default:null},ariaLabel:{type:String,default:null},ariaRoledescription:{type:String,default:null}},style:le,provide:function(){return{$pcGalleria:this,$parentInstance:this}}};function O(e){return me(e)||pe(e)||fe(e)||de()}function de(){throw TypeError(`Invalid attempt to spread non-iterable instance.
In order to be iterable, non-array objects must have a [Symbol.iterator]() method.`)}function fe(e,t){if(e){if(typeof e==`string`)return k(e,t);var n={}.toString.call(e).slice(8,-1);return n===`Object`&&e.constructor&&(n=e.constructor.name),n===`Map`||n===`Set`?Array.from(e):n===`Arguments`||/^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(n)?k(e,t):void 0}}function pe(e){if(typeof Symbol<`u`&&e[Symbol.iterator]!=null||e[`@@iterator`]!=null)return Array.from(e)}function me(e){if(Array.isArray(e))return k(e)}function k(e,t){(t==null||t>e.length)&&(t=e.length);for(var n=0,r=Array(t);n<t;n++)r[n]=e[n];return r}var A={name:`GalleriaItem`,hostName:`Galleria`,extends:b,emits:[`start-slideshow`,`stop-slideshow`,`update:activeIndex`],props:{circular:{type:Boolean,default:!1},activeIndex:{type:Number,default:0},value:{type:Array,default:null},showItemNavigators:{type:Boolean,default:!0},showIndicators:{type:Boolean,default:!0},slideShowActive:{type:Boolean,default:!0},changeItemOnIndicatorHover:{type:Boolean,default:!0},autoPlay:{type:Boolean,default:!1},templates:{type:null,default:null},id:{type:String,default:null}},mounted:function(){this.autoPlay&&this.$emit(`start-slideshow`)},methods:{getIndicatorPTOptions:function(e){return{context:{highlighted:this.activeIndex===e}}},next:function(){var e=this.activeIndex+1,t=this.circular&&this.value.length-1===this.activeIndex?0:e;this.$emit(`update:activeIndex`,t)},prev:function(){var e=this.activeIndex===0?0:this.activeIndex-1,t=this.circular&&this.activeIndex===0?this.value.length-1:e;this.$emit(`update:activeIndex`,t)},stopSlideShow:function(){this.slideShowActive&&this.stopSlideShow&&this.$emit(`stop-slideshow`)},navBackward:function(e){this.stopSlideShow(),this.prev(),e&&e.cancelable&&e.preventDefault()},navForward:function(e){this.stopSlideShow(),this.next(),e&&e.cancelable&&e.preventDefault()},onIndicatorClick:function(e){this.stopSlideShow(),this.$emit(`update:activeIndex`,e)},onIndicatorMouseEnter:function(e){this.changeItemOnIndicatorHover&&(this.stopSlideShow(),this.$emit(`update:activeIndex`,e))},onIndicatorKeyDown:function(e,t){switch(e.code){case`Enter`:case`NumpadEnter`:case`Space`:this.stopSlideShow(),this.$emit(`update:activeIndex`,t),e.preventDefault();break;case`ArrowRight`:this.onRightKey();break;case`ArrowLeft`:this.onLeftKey();break;case`Home`:this.onHomeKey(),e.preventDefault();break;case`End`:this.onEndKey(),e.preventDefault();break;case`Tab`:this.onTabKey();break;case`ArrowDown`:case`ArrowUp`:case`PageUp`:case`PageDown`:e.preventDefault()}},onRightKey:function(){var e=O(g(this.$refs.indicatorContent,`[data-pc-section="indicator"]`)),t=this.findFocusedIndicatorIndex();this.changedFocusedIndicator(t,t+1===e.length?e.length-1:t+1)},onLeftKey:function(){var e=this.findFocusedIndicatorIndex();this.changedFocusedIndicator(e,e-1<=0?0:e-1)},onHomeKey:function(){var e=this.findFocusedIndicatorIndex();this.changedFocusedIndicator(e,0)},onEndKey:function(){var e=O(g(this.$refs.indicatorContent,`[data-pc-section="indicator"]`)),t=this.findFocusedIndicatorIndex();this.changedFocusedIndicator(t,e.length-1)},onTabKey:function(){var e=O(g(this.$refs.indicatorContent,`[data-pc-section="indicator"]`)),t=e.findIndex(function(e){return m(e,`data-p-active`)===!0}),n=y(this.$refs.indicatorContent,`[data-pc-section="indicator"] > [tabindex="0"]`),r=e.findIndex(function(e){return e===n.parentElement});e[r].children[0].tabIndex=`-1`,e[t].children[0].tabIndex=`0`},findFocusedIndicatorIndex:function(){var e=O(g(this.$refs.indicatorContent,`[data-pc-section="indicator"]`)),t=y(this.$refs.indicatorContent,`[data-pc-section="indicator"] > [tabindex="0"]`);return e.findIndex(function(e){return e===t.parentElement})},changedFocusedIndicator:function(e,t){var n=O(g(this.$refs.indicatorContent,`[data-pc-section="indicator"]`));n[e].children[0].tabIndex=`-1`,n[t].children[0].tabIndex=`0`,n[t].children[0].focus()},isIndicatorItemActive:function(e){return this.activeIndex===e},ariaSlideNumber:function(e){return this.$primevue.config.locale.aria?this.$primevue.config.locale.aria.slideNumber.replace(/{slideNumber}/g,e):void 0},ariaPageLabel:function(e){return this.$primevue.config.locale.aria?this.$primevue.config.locale.aria.pageLabel.replace(/{page}/g,e):void 0}},computed:{activeItem:function(){return this.value[this.activeIndex]},ariaSlideLabel:function(){return this.$primevue.config.locale.aria?this.$primevue.config.locale.aria.slide:void 0},isNavBackwardDisabled:function(){return!this.circular&&this.activeIndex===0},isNavForwardDisabled:function(){return!this.circular&&this.activeIndex===this.value.length-1}},components:{ChevronLeftIcon:D,ChevronRightIcon:E},directives:{ripple:S}},j=[`disabled`],M=[`id`,`aria-label`,`aria-roledescription`],N=[`disabled`],P=[`aria-label`,`aria-selected`,`aria-controls`,`onClick`,`onMouseenter`,`onKeydown`,`data-p-active`],F=[`tabindex`];function I(r,u,f,ee,m,h){var g=e(`ripple`);return p(),c(`div`,o({class:r.cx(`itemsContainer`)},r.ptm(`itemsContainer`)),[d(`div`,o({class:r.cx(`items`)},r.ptm(`items`)),[f.showItemNavigators?s((p(),c(`button`,o({key:0,type:`button`,class:r.cx(`prevButton`),onClick:u[0]||=function(e){return h.navBackward(e)},disabled:h.isNavBackwardDisabled},r.ptm(`prevButton`),{"data-pc-group-section":`itemnavigator`}),[(p(),i(n(f.templates.previousitemicon||`ChevronLeftIcon`),o({class:r.cx(`prevIcon`)},r.ptm(`prevIcon`)),null,16,[`class`]))],16,j)),[[g]]):a(``,!0),d(`div`,o({id:f.id+`_item_`+f.activeIndex,class:r.cx(`item`),role:`group`,"aria-label":h.ariaSlideNumber(f.activeIndex+1),"aria-roledescription":h.ariaSlideLabel},r.ptm(`item`)),[f.templates.item?(p(),i(n(f.templates.item),{key:0,item:h.activeItem},null,8,[`item`])):a(``,!0)],16,M),f.showItemNavigators?s((p(),c(`button`,o({key:1,type:`button`,class:r.cx(`nextButton`),onClick:u[1]||=function(e){return h.navForward(e)},disabled:h.isNavForwardDisabled},r.ptm(`nextButton`),{"data-pc-group-section":`itemnavigator`}),[(p(),i(n(f.templates.nextitemicon||`ChevronRightIcon`),o({class:r.cx(`nextIcon`)},r.ptm(`nextIcon`)),null,16,[`class`]))],16,N)),[[g]]):a(``,!0),f.templates.caption?(p(),c(`div`,o({key:2,class:r.cx(`caption`)},r.ptm(`caption`)),[f.templates.caption?(p(),i(n(f.templates.caption),{key:0,item:h.activeItem},null,8,[`item`])):a(``,!0)],16)):a(``,!0)],16),f.showIndicators?(p(),c(`ul`,o({key:0,ref:`indicatorContent`,class:r.cx(`indicatorList`)},r.ptm(`indicatorList`)),[(p(!0),c(l,null,t(f.value,function(e,t){return p(),c(`li`,o({key:`p-galleria-indicator-${t}`,class:r.cx(`indicator`,{index:t}),"aria-label":h.ariaPageLabel(t+1),"aria-selected":f.activeIndex===t,"aria-controls":f.id+`_item_`+t,onClick:function(e){return h.onIndicatorClick(t)},onMouseenter:function(e){return h.onIndicatorMouseEnter(t)},onKeydown:function(e){return h.onIndicatorKeyDown(e,t)}},{ref_for:!0},r.ptm(`indicator`,h.getIndicatorPTOptions(t)),{"data-p-active":h.isIndicatorItemActive(t)}),[f.templates.indicator?a(``,!0):(p(),c(`button`,o({key:0,type:`button`,tabindex:f.activeIndex===t?`0`:`-1`,class:r.cx(`indicatorButton`)},{ref_for:!0},r.ptm(`indicatorButton`,h.getIndicatorPTOptions(t))),null,16,F)),f.templates.indicator?(p(),i(n(f.templates.indicator),{key:1,index:t,activeIndex:f.activeIndex,tabindex:f.activeIndex===t?`0`:`-1`},null,8,[`index`,`activeIndex`,`tabindex`])):a(``,!0)],16,P)}),128))],16)):a(``,!0)],16)}A.render=I;function L(e){return ge(e)||he(e)||z(e)||R()}function R(){throw TypeError(`Invalid attempt to spread non-iterable instance.
In order to be iterable, non-array objects must have a [Symbol.iterator]() method.`)}function z(e,t){if(e){if(typeof e==`string`)return B(e,t);var n={}.toString.call(e).slice(8,-1);return n===`Object`&&e.constructor&&(n=e.constructor.name),n===`Map`||n===`Set`?Array.from(e):n===`Arguments`||/^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(n)?B(e,t):void 0}}function he(e){if(typeof Symbol<`u`&&e[Symbol.iterator]!=null||e[`@@iterator`]!=null)return Array.from(e)}function ge(e){if(Array.isArray(e))return B(e)}function B(e,t){(t==null||t>e.length)&&(t=e.length);for(var n=0,r=Array(t);n<t;n++)r[n]=e[n];return r}var V={name:`GalleriaThumbnails`,hostName:`Galleria`,extends:b,emits:[`stop-slideshow`,`update:activeIndex`],props:{containerId:{type:String,default:null},value:{type:Array,default:null},numVisible:{type:Number,default:3},activeIndex:{type:Number,default:0},isVertical:{type:Boolean,default:!1},slideShowActive:{type:Boolean,default:!1},circular:{type:Boolean,default:!1},responsiveOptions:{type:Array,default:null},contentHeight:{type:String,default:`300px`},showThumbnailNavigators:{type:Boolean,default:!0},templates:{type:null,default:null},prevButtonProps:{type:null,default:null},nextButtonProps:{type:null,default:null}},startPos:null,thumbnailsStyle:null,sortedResponsiveOptions:null,data:function(){return{d_numVisible:this.numVisible,d_oldNumVisible:this.numVisible,d_activeIndex:this.activeIndex,d_oldActiveItemIndex:this.activeIndex,totalShiftedItems:0,page:0}},watch:{numVisible:function(e,t){this.d_numVisible=e,this.d_oldNumVisible=t},activeIndex:function(e,t){this.d_activeIndex=e,this.d_oldActiveItemIndex=t}},mounted:function(){this.createStyle(),this.calculatePosition(),this.responsiveOptions&&this.bindDocumentListeners()},updated:function(){var e=this.totalShiftedItems;(this.d_oldNumVisible!==this.d_numVisible||this.d_oldActiveItemIndex!==this.d_activeIndex)&&(e=this.d_activeIndex<=this.getMedianItemIndex()?0:this.value.length-this.d_numVisible+this.getMedianItemIndex()<this.d_activeIndex?this.d_numVisible-this.value.length:this.value.length-this.d_numVisible<this.d_activeIndex&&this.d_numVisible%2==0?this.d_activeIndex*-1+this.getMedianItemIndex()+1:this.d_activeIndex*-1+this.getMedianItemIndex(),e!==this.totalShiftedItems&&(this.totalShiftedItems=e),this.$refs.itemsContainer.style.transform=this.isVertical?`translate3d(0, ${e*(100/this.d_numVisible)}%, 0)`:`translate3d(${e*(100/this.d_numVisible)}%, 0, 0)`,this.d_oldActiveItemIndex!==this.d_activeIndex&&(document.body.setAttribute(`data-p-items-hidden`,`false`),!this.isUnstyled&&v(this.$refs.itemsContainer,`p-items-hidden`),this.$refs.itemsContainer.style.transition=`transform 500ms ease 0s`),this.d_oldActiveItemIndex=this.d_activeIndex,this.d_oldNumVisible=this.d_numVisible)},beforeUnmount:function(){this.responsiveOptions&&this.unbindDocumentListeners(),this.thumbnailsStyle&&this.thumbnailsStyle.parentNode.removeChild(this.thumbnailsStyle)},methods:{step:function(e){var t=this.totalShiftedItems+e;e<0&&-1*t+this.d_numVisible>this.value.length-1?t=this.d_numVisible-this.value.length:e>0&&t>0&&(t=0),this.circular&&(e<0&&this.value.length-1===this.d_activeIndex?t=0:e>0&&this.d_activeIndex===0&&(t=this.d_numVisible-this.value.length)),this.$refs.itemsContainer&&(document.body.setAttribute(`data-p-items-hidden`,`false`),!this.isUnstyled&&v(this.$refs.itemsContainer,`p-items-hidden`),this.$refs.itemsContainer.style.transform=this.isVertical?`translate3d(0, ${t*(100/this.d_numVisible)}%, 0)`:`translate3d(${t*(100/this.d_numVisible)}%, 0, 0)`,this.$refs.itemsContainer.style.transition=`transform 500ms ease 0s`),this.totalShiftedItems=t},stopSlideShow:function(){this.slideShowActive&&this.stopSlideShow&&this.$emit(`stop-slideshow`)},getMedianItemIndex:function(){var e=Math.floor(this.d_numVisible/2);return this.d_numVisible%2?e:e-1},navBackward:function(e){this.stopSlideShow();var t=this.d_activeIndex===0?0:this.d_activeIndex-1,n=t+this.totalShiftedItems;this.d_numVisible-n-1>this.getMedianItemIndex()&&(-1*this.totalShiftedItems!=0||this.circular)&&this.step(1);var r=this.circular&&this.d_activeIndex===0?this.value.length-1:t;this.$emit(`update:activeIndex`,r),e.cancelable&&e.preventDefault()},navForward:function(e){this.stopSlideShow();var t=this.d_activeIndex===this.value.length-1?this.value.length-1:this.d_activeIndex+1;t+this.totalShiftedItems>this.getMedianItemIndex()&&(-1*this.totalShiftedItems<this.getTotalPageNumber()-1||this.circular)&&this.step(-1);var n=this.circular&&this.value.length-1===this.d_activeIndex?0:t;this.$emit(`update:activeIndex`,n),e.cancelable&&e.preventDefault()},onItemClick:function(e){this.stopSlideShow();var t=e;if(t!==this.d_activeIndex){var n=t+this.totalShiftedItems,r=0;t<this.d_activeIndex?(r=this.d_numVisible-n-1-this.getMedianItemIndex(),r>0&&-1*this.totalShiftedItems!=0&&this.step(r)):(r=this.getMedianItemIndex()-n,r<0&&-1*this.totalShiftedItems<this.getTotalPageNumber()-1&&this.step(r)),this.$emit(`update:activeIndex`,t)}},onThumbnailKeydown:function(e,t){switch((e.code===`Enter`||e.code===`NumpadEnter`||e.code===`Space`)&&(this.onItemClick(t),e.preventDefault()),e.code){case`ArrowRight`:this.onRightKey();break;case`ArrowLeft`:this.onLeftKey();break;case`Home`:this.onHomeKey(),e.preventDefault();break;case`End`:this.onEndKey(),e.preventDefault();break;case`ArrowUp`:case`ArrowDown`:e.preventDefault();break;case`Tab`:this.onTabKey()}},onRightKey:function(){var e=g(this.$refs.itemsContainer,`[data-pc-section="thumbnailitem"]`),t=this.findFocusedIndicatorIndex();this.changedFocusedIndicator(t,t+1===e.length?e.length-1:t+1)},onLeftKey:function(){var e=this.findFocusedIndicatorIndex();this.changedFocusedIndicator(e,e-1<=0?0:e-1)},onHomeKey:function(){var e=this.findFocusedIndicatorIndex();this.changedFocusedIndicator(e,0)},onEndKey:function(){var e=g(this.$refs.itemsContainer,`[data-pc-section="thumbnailitem"]`),t=this.findFocusedIndicatorIndex();this.changedFocusedIndicator(t,e.length-1)},onTabKey:function(){var e=L(g(this.$refs.itemsContainer,`[data-pc-section="thumbnailitem"]`)),t=e.findIndex(function(e){return m(e,`data-p-active`)===!0}),n=y(this.$refs.itemsContainer,`[tabindex="0"]`),r=e.findIndex(function(e){return e===n.parentElement});e[r].children[0].tabIndex=`-1`,e[t].children[0].tabIndex=`0`},findFocusedIndicatorIndex:function(){var e=L(g(this.$refs.itemsContainer,`[data-pc-section="thumbnailitem"]`)),t=y(this.$refs.itemsContainer,`[data-pc-section="thumbnailitem"] > [tabindex="0"]`);return e.findIndex(function(e){return e===t.parentElement})},changedFocusedIndicator:function(e,t){var n=g(this.$refs.itemsContainer,`[data-pc-section="thumbnailitem"]`);n[e].children[0].tabIndex=`-1`,n[t].children[0].tabIndex=`0`,n[t].children[0].focus()},onTransitionEnd:function(e){this.$refs.itemsContainer&&e.propertyName===`transform`&&(document.body.setAttribute(`data-p-items-hidden`,`true`),!this.isUnstyled&&_(this.$refs.itemsContainer,`p-items-hidden`),this.$refs.itemsContainer.style.transition=``)},onTouchStart:function(e){var t=e.changedTouches[0];this.startPos={x:t.pageX,y:t.pageY}},onTouchMove:function(e){e.cancelable&&e.preventDefault()},onTouchEnd:function(e){var t=e.changedTouches[0];this.isVertical?this.changePageOnTouch(e,t.pageY-this.startPos.y):this.changePageOnTouch(e,t.pageX-this.startPos.x)},changePageOnTouch:function(e,t){Math.abs(t)<10||(t<0?this.navForward(e):this.navBackward(e))},getTotalPageNumber:function(){return this.value.length>this.d_numVisible?this.value.length-this.d_numVisible+1:0},createStyle:function(){if(!this.thumbnailsStyle){var e;this.thumbnailsStyle=document.createElement(`style`),this.thumbnailsStyle.type=`text/css`,ie(this.thumbnailsStyle,`nonce`,(e=this.$primevue)==null||(e=e.config)==null||(e=e.csp)==null?void 0:e.nonce),document.body.appendChild(this.thumbnailsStyle)}var t=`
                #${this.containerId} [data-pc-section="thumbnailitem"] {
                    flex: 1 0 ${100/this.d_numVisible}%
                }
            `;if(this.responsiveOptions&&!this.isUnstyled){this.sortedResponsiveOptions=L(this.responsiveOptions);var n=te();this.sortedResponsiveOptions.sort(function(e,t){var r=e.breakpoint,i=t.breakpoint;return re(r,i,-1,n)});for(var r=0;r<this.sortedResponsiveOptions.length;r++){var i=this.sortedResponsiveOptions[r];t+=`
                        @media screen and (max-width: ${i.breakpoint}) {
                            #${this.containerId} .p-galleria-thumbnail-item {
                                flex: 1 0 ${100/i.numVisible}%
                            }
                        }
                    `}}this.thumbnailsStyle.innerHTML=t},calculatePosition:function(){if(this.$refs.itemsContainer&&this.sortedResponsiveOptions){for(var e=window.innerWidth,t={numVisible:this.numVisible},n=0;n<this.sortedResponsiveOptions.length;n++){var r=this.sortedResponsiveOptions[n];parseInt(r.breakpoint,10)>=e&&(t=r)}this.d_numVisible!==t.numVisible&&(this.d_numVisible=t.numVisible)}},bindDocumentListeners:function(){var e=this;this.documentResizeListener||(this.documentResizeListener=function(){e.calculatePosition()},window.addEventListener(`resize`,this.documentResizeListener))},unbindDocumentListeners:function(){this.documentResizeListener&&=(window.removeEventListener(`resize`,this.documentResizeListener),null)},firstItemAciveIndex:function(){return this.totalShiftedItems*-1},lastItemActiveIndex:function(){return this.firstItemAciveIndex()+this.d_numVisible-1},isItemActive:function(e){return this.firstItemAciveIndex()<=e&&this.lastItemActiveIndex()>=e},ariaPageLabel:function(e){return this.$primevue.config.locale.aria?this.$primevue.config.locale.aria.pageLabel.replace(/{page}/g,e):void 0}},computed:{ariaPrevButtonLabel:function(){return this.$primevue.config.locale.aria?this.$primevue.config.locale.aria.prevPageLabel:void 0},ariaNextButtonLabel:function(){return this.$primevue.config.locale.aria?this.$primevue.config.locale.aria.nextPageLabel:void 0},isNavBackwardDisabled:function(){return!this.circular&&this.d_activeIndex===0||this.value.length<=this.d_numVisible},isNavForwardDisabled:function(){return!this.circular&&this.d_activeIndex===this.value.length-1||this.value.length<=this.d_numVisible}},components:{ChevronLeftIcon:D,ChevronRightIcon:E,ChevronUpIcon:ce,ChevronDownIcon:se},directives:{ripple:S}};function H(e){"@babel/helpers - typeof";return H=typeof Symbol==`function`&&typeof Symbol.iterator==`symbol`?function(e){return typeof e}:function(e){return e&&typeof Symbol==`function`&&e.constructor===Symbol&&e!==Symbol.prototype?`symbol`:typeof e},H(e)}function U(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);t&&(r=r.filter(function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable})),n.push.apply(n,r)}return n}function W(e){for(var t=1;t<arguments.length;t++){var n=arguments[t]==null?{}:arguments[t];t%2?U(Object(n),!0).forEach(function(t){_e(e,t,n[t])}):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):U(Object(n)).forEach(function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))})}return e}function _e(e,t,n){return(t=ve(t))in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function ve(e){var t=ye(e,`string`);return H(t)==`symbol`?t:t+``}function ye(e,t){if(H(e)!=`object`||!e)return e;var n=e[Symbol.toPrimitive];if(n!==void 0){var r=n.call(e,t);if(H(r)!=`object`)return r;throw TypeError(`@@toPrimitive must return a primitive value.`)}return(t===`string`?String:Number)(e)}var be=[`disabled`,`aria-label`],xe=[`data-p-active`,`aria-selected`,`aria-controls`,`onKeydown`,`data-p-galleria-thumbnail-item-current`,`data-p-galleria-thumbnail-item-active`,`data-p-galleria-thumbnail-item-start`,`data-p-galleria-thumbnail-item-end`],Se=[`tabindex`,`aria-label`,`aria-current`,`onClick`],Ce=[`disabled`,`aria-label`];function we(r,u,f,ee,m,h){var g=e(`ripple`);return p(),c(`div`,o({class:r.cx(`thumbnails`)},r.ptm(`thumbnails`)),[d(`div`,o({class:r.cx(`thumbnailContent`)},r.ptm(`thumbnailContent`)),[f.showThumbnailNavigators?s((p(),c(`button`,o({key:0,class:r.cx(`thumbnailPrevButton`),disabled:h.isNavBackwardDisabled,type:`button`,"aria-label":h.ariaPrevButtonLabel,onClick:u[0]||=function(e){return h.navBackward(e)}},W(W({},f.prevButtonProps),r.ptm(`thumbnailPrevButton`)),{"data-pc-group-section":`thumbnailnavigator`}),[(p(),i(n(f.templates.previousthumbnailicon||(f.isVertical?`ChevronUpIcon`:`ChevronLeftIcon`)),o({class:r.cx(`thumbnailPrevIcon`)},r.ptm(`thumbnailPrevIcon`)),null,16,[`class`]))],16,be)),[[g]]):a(``,!0),d(`div`,o({class:r.cx(`thumbnailsViewport`),style:{height:f.isVertical?f.contentHeight:``}},r.ptm(`thumbnailsViewport`)),[d(`div`,o({ref:`itemsContainer`,class:r.cx(`thumbnailItems`),role:`tablist`,onTransitionend:u[1]||=function(e){return h.onTransitionEnd(e)},onTouchstart:u[2]||=function(e){return h.onTouchStart(e)},onTouchmove:u[3]||=function(e){return h.onTouchMove(e)},onTouchend:u[4]||=function(e){return h.onTouchEnd(e)}},r.ptm(`thumbnailItems`)),[(p(!0),c(l,null,t(f.value,function(e,t){return p(),c(`div`,o({key:`p-galleria-thumbnail-item-${t}`,class:r.cx(`thumbnailItem`,{index:t,activeIndex:f.activeIndex}),role:`tab`,"data-p-active":f.activeIndex===t,"aria-selected":f.activeIndex===t,"aria-controls":f.containerId+`_item_`+t,onKeydown:function(e){return h.onThumbnailKeydown(e,t)}},{ref_for:!0},r.ptm(`thumbnailItem`),{"data-p-galleria-thumbnail-item-current":f.activeIndex===t,"data-p-galleria-thumbnail-item-active":h.isItemActive(t),"data-p-galleria-thumbnail-item-start":h.firstItemAciveIndex()===t,"data-p-galleria-thumbnail-item-end":h.lastItemActiveIndex()===t}),[d(`div`,o({class:r.cx(`thumbnail`),tabindex:f.activeIndex===t?`0`:`-1`,"aria-label":h.ariaPageLabel(t+1),"aria-current":f.activeIndex===t?`page`:void 0,onClick:function(e){return h.onItemClick(t)}},{ref_for:!0},r.ptm(`thumbnail`)),[f.templates.thumbnail?(p(),i(n(f.templates.thumbnail),{key:0,item:e},null,8,[`item`])):a(``,!0)],16,Se)],16,xe)}),128))],16)],16),f.showThumbnailNavigators?s((p(),c(`button`,o({key:1,class:r.cx(`thumbnailNextButton`),disabled:h.isNavForwardDisabled,type:`button`,"aria-label":h.ariaNextButtonLabel,onClick:u[5]||=function(e){return h.navForward(e)}},W(W({},f.nextButtonProps),r.ptm(`thumbnailNextButton`)),{"data-pc-group-section":`thumbnailnavigator`}),[(p(),i(n(f.templates.nextthumbnailicon||(f.isVertical?`ChevronDownIcon`:`ChevronRightIcon`)),o({class:r.cx(`thumbnailNextIcon`)},r.ptm(`thumbnailNextIcon`)),null,16,[`class`]))],16,Ce)),[[g]]):a(``,!0)],16)],16)}V.render=we;function G(e){"@babel/helpers - typeof";return G=typeof Symbol==`function`&&typeof Symbol.iterator==`symbol`?function(e){return typeof e}:function(e){return e&&typeof Symbol==`function`&&e.constructor===Symbol&&e!==Symbol.prototype?`symbol`:typeof e},G(e)}function K(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);t&&(r=r.filter(function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable})),n.push.apply(n,r)}return n}function q(e){for(var t=1;t<arguments.length;t++){var n=arguments[t]==null?{}:arguments[t];t%2?K(Object(n),!0).forEach(function(t){Te(e,t,n[t])}):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):K(Object(n)).forEach(function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))})}return e}function Te(e,t,n){return(t=Ee(t))in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function Ee(e){var t=De(e,`string`);return G(t)==`symbol`?t:t+``}function De(e,t){if(G(e)!=`object`||!e)return e;var n=e[Symbol.toPrimitive];if(n!==void 0){var r=n.call(e,t);if(G(r)!=`object`)return r;throw TypeError(`@@toPrimitive must return a primitive value.`)}return(t===`string`?String:Number)(e)}var J={name:`GalleriaContent`,hostName:`Galleria`,extends:b,inheritAttrs:!1,interval:null,emits:[`activeitem-change`,`mask-hide`],data:function(){return{activeIndex:this.$attrs.activeIndex,numVisible:this.$attrs.numVisible,slideShowActive:!1}},watch:{"$attrs.value":function(e){e&&e.length<this.numVisible&&(this.numVisible=e.length)},"$attrs.activeIndex":function(e){this.activeIndex=e},"$attrs.numVisible":function(e){this.numVisible=e},"$attrs.autoPlay":function(e){e?this.startSlideShow():this.stopSlideShow()}},updated:function(){this.$emit(`activeitem-change`,this.activeIndex)},beforeUnmount:function(){this.slideShowActive&&this.stopSlideShow()},methods:{getPTOptions:function(e){return this.ptm(e,{props:q(q({},this.$attrs),{},{pt:this.pt,unstyled:this.unstyled})})},isAutoPlayActive:function(){return this.slideShowActive},startSlideShow:function(){var e=this;this.interval=setInterval(function(){e.activeIndex=e.$attrs.circular&&e.$attrs.value.length-1===e.activeIndex?0:e.activeIndex+1},this.$attrs.transitionInterval),this.slideShowActive=!0},stopSlideShow:function(){this.interval&&clearInterval(this.interval),this.slideShowActive=!1},getPositionClass:function(e,t){var n=[`top`,`left`,`bottom`,`right`].find(function(e){return e===t});return n?`${e}-${n}`:``},isVertical:function(){return this.$attrs.thumbnailsPosition===`left`||this.$attrs.thumbnailsPosition===`right`}},computed:{closeAriaLabel:function(){return this.$primevue.config.locale.aria?this.$primevue.config.locale.aria.close:void 0}},components:{GalleriaItem:A,GalleriaThumbnails:V,TimesIcon:C},directives:{ripple:S}};function Y(e){"@babel/helpers - typeof";return Y=typeof Symbol==`function`&&typeof Symbol.iterator==`symbol`?function(e){return typeof e}:function(e){return e&&typeof Symbol==`function`&&e.constructor===Symbol&&e!==Symbol.prototype?`symbol`:typeof e},Y(e)}function X(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);t&&(r=r.filter(function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable})),n.push.apply(n,r)}return n}function Z(e){for(var t=1;t<arguments.length;t++){var n=arguments[t]==null?{}:arguments[t];t%2?X(Object(n),!0).forEach(function(t){Oe(e,t,n[t])}):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):X(Object(n)).forEach(function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))})}return e}function Oe(e,t,n){return(t=ke(t))in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function ke(e){var t=Ae(e,`string`);return Y(t)==`symbol`?t:t+``}function Ae(e,t){if(Y(e)!=`object`||!e)return e;var n=e[Symbol.toPrimitive];if(n!==void 0){var r=n.call(e,t);if(Y(r)!=`object`)return r;throw TypeError(`@@toPrimitive must return a primitive value.`)}return(t===`string`?String:Number)(e)}var je=[`id`,`aria-label`,`aria-roledescription`],Q=[`aria-label`],Me=[`aria-live`];function Ne(t,l,u,ee,m,h){var g=r(`GalleriaItem`),_=r(`GalleriaThumbnails`),v=e(`ripple`);return t.$attrs.value&&t.$attrs.value.length>0?(p(),c(`div`,o({key:0,id:t.$id,role:`region`,class:[t.cx(`root`),t.$attrs.containerClass],style:t.$attrs.containerStyle,"aria-label":t.$attrs.ariaLabel,"aria-roledescription":t.$attrs.ariaRoledescription},Z(Z({},t.$attrs.containerProps),h.getPTOptions(`root`))),[t.$attrs.fullScreen?s((p(),c(`button`,o({key:0,autofocus:``,type:`button`,class:t.cx(`closeButton`),"aria-label":h.closeAriaLabel,onClick:l[0]||=function(e){return t.$emit(`mask-hide`)}},h.getPTOptions(`closeButton`)),[(p(),i(n(t.$attrs.templates.closeicon||`TimesIcon`),o({class:t.cx(`closeIcon`)},h.getPTOptions(`closeIcon`)),null,16,[`class`]))],16,Q)),[[v]]):a(``,!0),t.$attrs.templates&&t.$attrs.templates.header?(p(),c(`div`,o({key:1,class:t.cx(`header`)},h.getPTOptions(`header`)),[(p(),i(n(t.$attrs.templates.header)))],16)):a(``,!0),d(`div`,o({class:t.cx(`content`),"aria-live":t.$attrs.autoPlay?`polite`:`off`},h.getPTOptions(`content`)),[f(g,{id:t.$id,activeIndex:m.activeIndex,"onUpdate:activeIndex":l[1]||=function(e){return m.activeIndex=e},slideShowActive:m.slideShowActive,"onUpdate:slideShowActive":l[2]||=function(e){return m.slideShowActive=e},value:t.$attrs.value,circular:t.$attrs.circular,templates:t.$attrs.templates,showIndicators:t.$attrs.showIndicators,changeItemOnIndicatorHover:t.$attrs.changeItemOnIndicatorHover,showItemNavigators:t.$attrs.showItemNavigators,autoPlay:t.$attrs.autoPlay,onStartSlideshow:h.startSlideShow,onStopSlideshow:h.stopSlideShow,pt:t.pt,unstyled:t.unstyled},null,8,[`id`,`activeIndex`,`slideShowActive`,`value`,`circular`,`templates`,`showIndicators`,`changeItemOnIndicatorHover`,`showItemNavigators`,`autoPlay`,`onStartSlideshow`,`onStopSlideshow`,`pt`,`unstyled`]),t.$attrs.showThumbnails?(p(),i(_,{key:0,activeIndex:m.activeIndex,"onUpdate:activeIndex":l[3]||=function(e){return m.activeIndex=e},slideShowActive:m.slideShowActive,"onUpdate:slideShowActive":l[4]||=function(e){return m.slideShowActive=e},containerId:t.$id,value:t.$attrs.value,templates:t.$attrs.templates,numVisible:m.numVisible,responsiveOptions:t.$attrs.responsiveOptions,circular:t.$attrs.circular,isVertical:h.isVertical(),contentHeight:t.$attrs.verticalThumbnailViewPortHeight,showThumbnailNavigators:t.$attrs.showThumbnailNavigators,prevButtonProps:t.$attrs.prevButtonProps,nextButtonProps:t.$attrs.nextButtonProps,onStopSlideshow:h.stopSlideShow,pt:t.pt,unstyled:t.unstyled},null,8,[`activeIndex`,`slideShowActive`,`containerId`,`value`,`templates`,`numVisible`,`responsiveOptions`,`circular`,`isVertical`,`contentHeight`,`showThumbnailNavigators`,`prevButtonProps`,`nextButtonProps`,`onStopSlideshow`,`pt`,`unstyled`])):a(``,!0)],16,Me),t.$attrs.templates&&t.$attrs.templates.footer?(p(),c(`div`,o({key:2,class:t.cx(`footer`)},h.getPTOptions(`footer`)),[(p(),i(n(t.$attrs.templates.footer)))],16)):a(``,!0)],16,je)):a(``,!0)}J.render=Ne;var $={name:`Galleria`,extends:ue,inheritAttrs:!1,emits:[`update:activeIndex`,`update:visible`],container:null,mask:null,documentKeydownListener:null,data:function(){return{containerVisible:this.visible,target:null}},updated:function(){this.fullScreen&&this.visible&&(this.containerVisible=this.visible)},beforeUnmount:function(){this.fullScreen&&T(),this.mask=null,this.container&&=(x.clear(this.container.$el||this.container),null)},methods:{onBeforeEnter:function(e){x.set(`modal`,e,this.baseZIndex||this.$primevue.config.zIndex.modal)},onEnter:function(e){this.target=document.activeElement,this.mask.style.zIndex=String(parseInt(e.style.zIndex,10)-1),oe(),this.focus(),this.bindGlobalListeners()},onBeforeLeave:function(){!this.isUnstyled&&_(this.mask,`p-overlay-mask-leave-active`)},onLeave:function(){h(this.target),this.target=null},onAfterLeave:function(e){x.clear(e),this.containerVisible=!1,T(),this.unbindGlobalListeners()},onActiveItemChange:function(e){this.activeIndex!==e&&this.$emit(`update:activeIndex`,e)},maskHide:function(){this.$emit(`update:visible`,!1)},containerRef:function(e){this.container=e},maskRef:function(e){this.mask=e},onKeyDown:function(e){e.code===`Escape`&&this.maskHide()},bindDocumentKeyDownListener:function(){this.documentKeydownListener||(this.documentKeydownListener=this.onKeyDown.bind(this),window.document.addEventListener(`keydown`,this.documentKeydownListener))},unbindDocumentKeyDownListener:function(){this.documentKeydownListener&&=(window.document.removeEventListener(`keydown`,this.documentKeydownListener),null)},bindGlobalListeners:function(){this.fullScreen&&this.bindDocumentKeyDownListener()},unbindGlobalListeners:function(){this.fullScreen&&this.unbindDocumentKeyDownListener()},focus:function(){var e=this.container.$el.querySelector(`[autofocus]`);e&&e.focus()}},components:{GalleriaContent:J,Portal:ae},directives:{focustrap:w}},Pe=[`aria-modal`];function Fe(t,n,l,d,m,h){var g=r(`GalleriaContent`),_=r(`Portal`),v=e(`focustrap`);return t.fullScreen?(p(),i(_,{key:0},{default:u(function(){return[m.containerVisible?(p(),c(`div`,o({key:0,ref:h.maskRef,class:[t.cx(`mask`),t.maskClass],role:`dialog`,"aria-modal":t.fullScreen?`true`:void 0},t.ptm(`mask`)),[f(ee,o({name:`p-galleria`,onBeforeEnter:h.onBeforeEnter,onEnter:h.onEnter,onBeforeLeave:h.onBeforeLeave,onLeave:h.onLeave,onAfterLeave:h.onAfterLeave,appear:``},t.ptm(`transition`)),{default:u(function(){return[t.visible?s((p(),i(g,o({key:0,ref:h.containerRef,onMaskHide:h.maskHide,templates:t.$slots,onActiveitemChange:h.onActiveItemChange,pt:t.pt,unstyled:t.unstyled},t.$props),null,16,[`onMaskHide`,`templates`,`onActiveitemChange`,`pt`,`unstyled`])),[[v]]):a(``,!0)]}),_:1},16,[`onBeforeEnter`,`onEnter`,`onBeforeLeave`,`onLeave`,`onAfterLeave`])],16,Pe)):a(``,!0)]}),_:1})):(p(),i(g,o({key:1,templates:t.$slots,onActiveitemChange:h.onActiveItemChange,pt:t.pt,unstyled:t.unstyled},t.$props),null,16,[`templates`,`onActiveitemChange`,`pt`,`unstyled`]))}$.render=Fe;export{$ as default};