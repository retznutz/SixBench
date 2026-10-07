import{n as e}from"./hePW80VL.js";import{It as t,K as n,U as r,W as i,d as a,f as o,k as s,nt as c,p as l,tt as u,u as d,v as f,z as p,zt as m}from"./DxfkHh_f.js";import{o as h,t as g}from"./D5TQhe1R.js";import{r as _,t as v}from"./Cxvj1CVF.js";import{t as y}from"./CRHlWn3X.js";import{t as b}from"./DCLXnpaw.js";import{n as x}from"./DCgt8QoJ.js";import{t as S}from"./CbNga7rs.js";import{t as C}from"./g_bVQVzv.js";var w=_.extend({name:`panel`,style:`
    .p-panel {
        display: block;
        border: 1px solid dt('panel.border.color');
        border-radius: dt('panel.border.radius');
        background: dt('panel.background');
        color: dt('panel.color');
    }

    .p-panel-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: dt('panel.header.padding');
        background: dt('panel.header.background');
        color: dt('panel.header.color');
        border-style: solid;
        border-width: dt('panel.header.border.width');
        border-color: dt('panel.header.border.color');
        border-radius: dt('panel.header.border.radius');
    }

    .p-panel-toggleable .p-panel-header {
        padding: dt('panel.toggleable.header.padding');
    }

    .p-panel-title {
        line-height: 1;
        font-weight: dt('panel.title.font.weight');
    }

    .p-panel-content-container {
        display: grid;
        grid-template-rows: 1fr;
    }

    .p-panel-content-wrapper {
        min-height: 0;
    }

    .p-panel-content {
        padding: dt('panel.content.padding');
    }

    .p-panel-footer {
        padding: dt('panel.footer.padding');
    }
`,classes:{root:function(e){return[`p-panel p-component`,{"p-panel-toggleable":e.props.toggleable}]},header:`p-panel-header`,title:`p-panel-title`,headerActions:`p-panel-header-actions`,pcToggleButton:`p-panel-toggle-button`,contentContainer:`p-panel-content-container`,contentWrapper:`p-panel-content-wrapper`,content:`p-panel-content`,footer:`p-panel-footer`}}),T=e({default:()=>E}),E={name:`Panel`,extends:{name:`BasePanel`,extends:v,props:{header:String,toggleable:Boolean,collapsed:Boolean,toggleButtonProps:{type:Object,default:function(){return{severity:`secondary`,text:!0,rounded:!0}}}},style:w,provide:function(){return{$pcPanel:this,$parentInstance:this}}},inheritAttrs:!1,emits:[`update:collapsed`,`toggle`],data:function(){return{d_collapsed:this.collapsed}},watch:{collapsed:function(e){this.d_collapsed=e}},methods:{toggle:function(e){this.d_collapsed=!this.d_collapsed,this.$emit(`update:collapsed`,this.d_collapsed),this.$emit(`toggle`,{originalEvent:e,value:this.d_collapsed})},onKeyDown:function(e){(e.code===`Enter`||e.code===`NumpadEnter`||e.code===`Space`)&&(this.toggle(e),e.preventDefault())}},computed:{buttonAriaLabel:function(){return this.toggleButtonProps&&this.toggleButtonProps.ariaLabel?this.toggleButtonProps.ariaLabel:this.header},dataP:function(){return y({toggleable:this.toggleable})}},components:{PlusIcon:C,MinusIcon:S,Button:x},directives:{ripple:b}},D=[`data-p`],O=[`data-p`],k=[`id`],A=[`id`,`aria-labelledby`];function j(e,_,v,y,b,x){var S=i(`Button`);return p(),l(`div`,s({class:e.cx(`root`),"data-p":x.dataP},e.ptmi(`root`)),[d(`div`,s({class:e.cx(`header`),"data-p":x.dataP},e.ptm(`header`)),[r(e.$slots,`header`,{id:e.$id+`_header`,class:t(e.cx(`title`)),collapsed:b.d_collapsed},function(){return[e.header?(p(),l(`span`,s({key:0,id:e.$id+`_header`,class:e.cx(`title`)},e.ptm(`title`)),m(e.header),17,k)):o(``,!0)]}),d(`div`,s({class:e.cx(`headerActions`)},e.ptm(`headerActions`)),[r(e.$slots,`icons`),e.toggleable?r(e.$slots,`togglebutton`,{key:0,collapsed:b.d_collapsed,toggleCallback:function(e){return x.toggle(e)},keydownCallback:function(e){return x.onKeyDown(e)}},function(){return[f(S,s({id:e.$id+`_header`,class:e.cx(`pcToggleButton`),"aria-label":x.buttonAriaLabel,"aria-controls":e.$id+`_content`,"aria-expanded":!b.d_collapsed,unstyled:e.unstyled,onClick:_[0]||=function(e){return x.toggle(e)},onKeydown:_[1]||=function(e){return x.onKeyDown(e)}},e.toggleButtonProps,{pt:e.ptm(`pcToggleButton`)}),{icon:u(function(t){return[r(e.$slots,e.$slots.toggleicon?`toggleicon`:`togglericon`,{collapsed:b.d_collapsed},function(){return[(p(),a(n(b.d_collapsed?`PlusIcon`:`MinusIcon`),s({class:t.class},e.ptm(`pcToggleButton`).icon),null,16,[`class`]))]})]}),_:3},16,[`id`,`class`,`aria-label`,`aria-controls`,`aria-expanded`,`unstyled`,`pt`])]}):o(``,!0)],16)],16,O),f(g,s({name:`p-collapsible`},e.ptm(`transition`)),{default:u(function(){return[c(d(`div`,s({id:e.$id+`_content`,class:e.cx(`contentContainer`),role:`region`,"aria-labelledby":e.$id+`_header`},e.ptm(`contentContainer`)),[d(`div`,s({class:e.cx(`contentWrapper`)},e.ptm(`contentWrapper`)),[d(`div`,s({class:e.cx(`content`)},e.ptm(`content`)),[r(e.$slots,`default`)],16),e.$slots.footer?(p(),l(`div`,s({key:0,class:e.cx(`footer`)},e.ptm(`footer`)),[r(e.$slots,`footer`)],16)):o(``,!0)],16)],16,A),[[h,!b.d_collapsed]])]}),_:3},16)],16,D)}E.render=j;export{E as n,T as t};