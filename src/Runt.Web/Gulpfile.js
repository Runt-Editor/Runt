var gulp = require('gulp'),
    watch = require('gulp-watch'),
    rjs = require('gulp-requirejs'),
    plumber = require('gulp-plumber'),
    tcs = require('gulp-typescript-compiler');

gulp.task('default', function() {
  return watch({glob: 'ts/**/*.ts'})
    .pipe(plumber())
    .pipe(tcs({
      sourcemaps: true,
      module: 'amd'
    }))
    .pipe(gulp.dest('js'));
});
