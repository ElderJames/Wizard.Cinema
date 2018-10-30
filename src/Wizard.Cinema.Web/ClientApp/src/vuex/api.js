import Vue from 'vue';
import VueResource from 'vue-resource';
import { API_ROOT } from '../config.js';
import store from './store';
import router from '../router';
import { Toast } from 'mint-ui';
Vue.use(VueResource);

// HTTP相关
Vue.http.options.crossOrigin = true;
Vue.http.options.timeout = 30 * 1000;
Vue.http.options.xhr = { withCredentials: true };
Vue.http.options.root = API_ROOT; // api地址请求前缀

// 全局路由拦截
Vue.http.interceptors.push(function(request, next) {
    request.params = request.params || {};
    let version_code = store.state.user.version_code
    if (version_code) {
        request.params.version = version_code;
    }
    next(function(response) {
        if (response.ok) {
            if (response.status === 200) {
                // 正常返回
            } else if (response.data.error === 1) {
                // 假设该请求后端返回错误代码为1是需要登录的，则跳转至登录页面
                let {fullPath} = store.state.route;
                router.push({path: '/user/login', query: {redirect: fullPath}});
            } else if (response.data.error === 4) {
                // 参数错误
                // alert(response.data.message);
            } else if (response.data.error === 5) {
                // 程序异常
                alert(response.data.message);
            }
        } else {
            Toast('获取数据失败...');
            console.error(`${response.status}-${response.statusText}\n${response.url}`)
        }
    });
});
export default {
    login({ commit, state }, params) {
        return Vue.http.post('/api/account/signin', params);
    }
}
