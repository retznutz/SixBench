import{G as e,U as t,W as n,d as r,f as i,k as a,nt as o,p as s,tt as c,v as l,z as u}from"./DxfkHh_f.js";import{t as d}from"./D5TQhe1R.js";import{$ as f,H as p,L as m,P as h,d as g,k as _,o as v,r as y,t as b,y as x,z as S}from"./Cxvj1CVF.js";import{t as C}from"./cO8iUN-n.js";import{t as w}from"./DCLXnpaw.js";import{t as T}from"./BvzTRszk.js";import{t as E}from"./Bbd-WAcs.js";import{t as D}from"./qMI3xRT7.js";import{t as O}from"./CjaJiowr.js";var k=y.extend({name:`popover`,style:`
    .p-popover {
        margin-block-start: dt('popover.gutter');
        background: dt('popover.background');
        color: dt('popover.color');
        border: 1px solid dt('popover.border.color');
        border-radius: dt('popover.border.radius');
        box-shadow: dt('popover.shadow');
        will-change: transform;
    }

    .p-popover-content {
        padding: dt('popover.content.padding');
    }

    .p-popover-flipped {
        margin-block-start: calc(dt('popover.gutter') * -1);
        margin-block-end: dt('popover.gutter');
    }

    .p-popover:after,
    .p-popover:before {
        bottom: 100%;
        left: calc(dt('popover.arrow.offset') + dt('popover.arrow.left'));
        content: ' ';
        height: 0;
        width: 0;
        position: absolute;
        pointer-events: none;
    }

    .p-popover:after {
        border-width: calc(dt('popover.gutter') - 2px);
        margin-left: calc(-1 * (dt('popover.gutter') - 2px));
        border-style: solid;
        border-color: transparent;
        border-bottom-color: dt('popover.background');
    }

    .p-popover:before {
        border-width: dt('popover.gutter');
        margin-left: calc(-1 * dt('popover.gutter'));
        border-style: solid;
        border-color: transparent;
        border-bottom-color: dt('popover.border.color');
    }

    .p-popover-flipped:after,
    .p-popover-flipped:before {
        bottom: auto;
        top: 100%;
    }

    .p-popover.p-popover-flipped:after {
        border-bottom-color: transparent;
        border-top-color: dt('popover.background');
    }

    .p-popover.p-popover-flipped:before {
        border-bottom-color: transparent;
        border-top-color: dt('popover.border.color');
    }
`,classes:{root:`p-popover p-component`,content:`p-popover-content`}}),A={name:`Popover`,extends:{name:`BasePopover`,extends:b,props:{dismissable:{type:Boolean,default:!0},appendTo:{type:[String,Object],default:`body`},baseZIndex:{type:Number,default:0},autoZIndex:{type:Boolean,default:!0},breakpoints:{type:Object,default:null},closeOnEscape:{type:Boolean,default:!0}},style:k,provide:function(){return{$pcPopover:this,$parentInstance:this}}},inheritAttrs:!1,emits:[`show`,`hide`],data:function(){return{visible:!1}},watch:{dismissable:{immediate:!0,handler:function(e){e?this.bindOutsideClickListener():this.unbindOutsideClickListener()}}},selfClick:!1,target:null,eventTarget:null,outsideClickListener:null,scrollHandler:null,resizeListener:null,container:null,styleElement:null,overlayEventListener:null,documentKeydownListener:null,contentResizeObserver:null,beforeUnmount:function(){this.dismissable&&this.unbindOutsideClickListener(),this.scrollHandler&&=(this.scrollHandler.destroy(),null),this.destroyStyle(),this.unbindResizeListener(),this.unbindContentResizeListener(),this.target=null,this.container&&this.autoZIndex&&C.clear(this.container),this.overlayEventListener&&=(O.off(`overlay-click`,this.overlayEventListener),null),this.container=null},mounted:function(){this.breakpoints&&this.createStyle()},methods:{toggle:function(e,t){this.visible?this.hide():this.show(e,t)},show:function(e,t){this.visible=!0,this.eventTarget=e.currentTarget,this.target=t||e.currentTarget},hide:function(){this.visible=!1},onContentClick:function(){this.selfClick=!0},onEnter:function(e){var t=this;_(e,{position:`absolute`,top:`0`}),this.alignOverlay(),this.dismissable&&this.bindOutsideClickListener(),this.bindScrollListener(),this.bindResizeListener(),this.autoZIndex&&C.set(`overlay`,e,this.baseZIndex||this.$primevue.config.zIndex.overlay),this.overlayEventListener=function(e){t.container.contains(e.target)&&(t.selfClick=!0)},this.bindContentResizeListener(),this.focus(),O.on(`overlay-click`,this.overlayEventListener),this.$emit(`show`),this.closeOnEscape&&this.bindDocumentKeyDownListener()},onLeave:function(){this.unbindOutsideClickListener(),this.unbindScrollListener(),this.unbindResizeListener(),this.unbindDocumentKeyDownListener(),this.unbindContentResizeListener(),O.off(`overlay-click`,this.overlayEventListener),this.overlayEventListener=null,this.$emit(`hide`)},onAfterLeave:function(e){this.autoZIndex&&C.clear(e)},alignOverlay:function(){g(this.container,this.target,!1);var e=x(this.container),t=x(this.target),n=0;e.left<t.left&&(n=t.left-e.left),this.container.style.setProperty(v(`popover.arrow.left`).name,`${n}px`),e.top<t.top&&(this.container.setAttribute(`data-p-popover-flipped`,`true`),!this.isUnstyled&&h(this.container,`p-popover-flipped`))},onContentKeydown:function(e){e.code===`Escape`&&this.closeOnEscape&&(this.hide(),p(this.target))},onButtonKeydown:function(e){switch(e.code){case`ArrowDown`:case`ArrowUp`:case`ArrowLeft`:case`ArrowRight`:e.preventDefault()}},focus:function(){var e=this.container.querySelector(`[autofocus]`);e&&e.focus()},onKeyDown:function(e){e.code===`Escape`&&this.closeOnEscape&&(this.visible=!1)},bindDocumentKeyDownListener:function(){this.documentKeydownListener||(this.documentKeydownListener=this.onKeyDown.bind(this),window.document.addEventListener(`keydown`,this.documentKeydownListener))},unbindDocumentKeyDownListener:function(){this.documentKeydownListener&&=(window.document.removeEventListener(`keydown`,this.documentKeydownListener),null)},bindOutsideClickListener:function(){var e=this;!this.outsideClickListener&&f()&&(this.outsideClickListener=function(t){e.visible&&!e.selfClick&&!e.isTargetClicked(t)&&(e.visible=!1),e.selfClick=!1},document.addEventListener(`click`,this.outsideClickListener))},unbindOutsideClickListener:function(){this.outsideClickListener&&(document.removeEventListener(`click`,this.outsideClickListener),this.outsideClickListener=null,this.selfClick=!1)},bindScrollListener:function(){var e=this;this.scrollHandler||=new D(this.target,function(){e.visible&&=!1}),this.scrollHandler.bindScrollListener()},unbindScrollListener:function(){this.scrollHandler&&this.scrollHandler.unbindScrollListener()},bindResizeListener:function(){var e=this;this.resizeListener||(this.resizeListener=function(){e.visible&&!m()&&(e.visible=!1)},window.addEventListener(`resize`,this.resizeListener))},unbindResizeListener:function(){this.resizeListener&&=(window.removeEventListener(`resize`,this.resizeListener),null)},bindContentResizeListener:function(){var e=this;this.contentResizeObserver||(this.contentResizeObserver=new ResizeObserver(function(){e.visible&&e.alignOverlay()}),this.contentResizeObserver.observe(this.container))},unbindContentResizeListener:function(){this.contentResizeObserver&&=(this.contentResizeObserver.disconnect(),null)},isTargetClicked:function(e){return this.eventTarget&&(this.eventTarget===e.target||this.eventTarget.contains(e.target))},containerRef:function(e){this.container=e},createStyle:function(){if(!this.styleElement&&!this.isUnstyled){var e;this.styleElement=document.createElement(`style`),this.styleElement.type=`text/css`,S(this.styleElement,`nonce`,(e=this.$primevue)==null||(e=e.config)==null||(e=e.csp)==null?void 0:e.nonce),document.head.appendChild(this.styleElement);var t=``;for(var n in this.breakpoints)t+=`
                        @media screen and (max-width: ${n}) {
                            .p-popover[${this.$attrSelector}] {
                                width: ${this.breakpoints[n]} !important;
                            }
                        }
                    `;this.styleElement.innerHTML=t}},destroyStyle:function(){this.styleElement&&=(document.head.removeChild(this.styleElement),null)},onOverlayClick:function(e){O.emit(`overlay-click`,{originalEvent:e,target:this.target})}},directives:{focustrap:T,ripple:w},components:{Portal:E}},j=[`aria-modal`];function M(f,p,m,h,g,_){var v=n(`Portal`),y=e(`focustrap`);return u(),r(v,{appendTo:f.appendTo},{default:c(function(){return[l(d,a({name:`p-anchored-overlay`,onEnter:_.onEnter,onLeave:_.onLeave,onAfterLeave:_.onAfterLeave},f.ptm(`transition`)),{default:c(function(){return[g.visible?o((u(),s(`div`,a({key:0,ref:_.containerRef,role:`dialog`,"aria-modal":g.visible,onClick:p[3]||=function(){return _.onOverlayClick&&_.onOverlayClick.apply(_,arguments)},class:f.cx(`root`)},f.ptmi(`root`)),[f.$slots.container?t(f.$slots,`container`,{key:0,closeCallback:_.hide,keydownCallback:function(e){return _.onButtonKeydown(e)}}):(u(),s(`div`,a({key:1,class:f.cx(`content`),onClick:p[0]||=function(){return _.onContentClick&&_.onContentClick.apply(_,arguments)},onMousedown:p[1]||=function(){return _.onContentClick&&_.onContentClick.apply(_,arguments)},onKeydown:p[2]||=function(){return _.onContentKeydown&&_.onContentKeydown.apply(_,arguments)}},f.ptm(`content`)),[t(f.$slots,`default`)],16))],16,j)),[[y]]):i(``,!0)]}),_:3},16,[`onEnter`,`onLeave`,`onAfterLeave`])]}),_:3},8,[`appendTo`])}A.render=M;export{A as default};